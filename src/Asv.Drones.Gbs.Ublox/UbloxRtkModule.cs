using System.ComponentModel.Composition;
using System.Reactive.Linq;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Gbs.Core;
using Asv.Gnss;
using Asv.Mavlink;
using Asv.Mavlink.Server;
using Asv.Mavlink.V2.AsvGbs;
using Asv.Mavlink.V2.Common;
using Geodesy;
using NLog;

namespace Asv.Drones.Gbs.Ublox;

public class UbloxRtkModuleConfig
{
    public bool IsEnabled { get; set; } = true;
    public string ConnectionString { get; set; } = "serial:/dev/ttyACM0?br=115200";
    public int GbsStatusRateMs { get; set; } = 1000;
    public int UpdateStatusFromDeviceRateMs { get; set; } = 1000;
}

[Export(typeof(IModule))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class UbloxRtkModule:DisposableOnceWithCancel, IModule, IAsvGbsClient
{
    private readonly IGbsMavlinkService _svc;
    private readonly UbloxRtkModuleConfig _config;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly UbxDevice _device;
    private readonly RxValue<AsvGbsState> _state;
    private readonly RxValue<GeoPoint> _position;
    private int _busy;
    private bool _isInit;
    private int _updateStatusInProgress;
    private readonly RxValue<UbxNavSvin> _svIn;  

    [ImportingConstructor]
    public UbloxRtkModule(IGbsMavlinkService svc,IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        _config = configuration.Get<UbloxRtkModuleConfig>();
        // if disabled => do nothing
        if (_config.IsEnabled == false)
        {
            _logger.Warn("UBlox RTK module is disbaled and will be ignored");
            return;
        }
        
        // start to init device
        _svc.Server.StatusText.Info("Connecting to GNSS device...");
        _device = new UbxDevice(_config.ConnectionString).DisposeItWith(Disposable);
        _device.Connection.Filter<UbxNavSvin>().Subscribe(_svIn).DisposeItWith(Disposable);
        
        
        _state = new RxValue<AsvGbsState>(AsvGbsState.AsvGbsStateLoading).DisposeItWith(Disposable);
        _position = new RxValue<GeoPoint>(GeoPoint.Zero).DisposeItWith(Disposable);
        
        _svc.UpdateCustomMode(_=>_.Compatibility |= AsvGbsCompatibility.RtkMode );
        _svc.Server.Gbs.Init(TimeSpan.FromMilliseconds(_config.GbsStatusRateMs),this );

        Observable.Timer(TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs), TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs))
            .Subscribe(UpdateStatus)
            .DisposeItWith(Disposable);

    }

    private async void UpdateStatus(long l)
    {
        if (Interlocked.CompareExchange(ref _updateStatusInProgress,1,0) !=0) return;
        try
        {
            if (_isInit == false) return;
            
            var cfgTMode3 = await _device.GetCfgTMode3(CancellationToken.None);
            switch (cfgTMode3.Mode)
            {
                case TMode3Enum.Disabled:
                    _state.OnNext(AsvGbsState.AsvGbsStateIdleMode);
                    break;
                case TMode3Enum.SurveyIn:
                    _state.OnNext(_svIn.Value.Active
                        ? AsvGbsState.AsvGbsStateAutoModeInProgress
                        : AsvGbsState.AsvGbsStateAutoMode);
                    _position.OnNext(_svIn.Value.Location ?? GeoPoint.Zero);
                    break;
                case TMode3Enum.FixedMode:
                    _state.OnNext(AsvGbsState.AsvGbsStateFixedMode);
                    _position.OnNext(cfgTMode3.Location ?? GeoPoint.Zero);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _updateStatusInProgress, 0);
        }
    }

    

    #region Init

    public void Init()
    {
        // if disabled => do nothing
        if (_config.IsEnabled == false) return;
        Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(_ => InitUbxDevice(),DisposeCancel);
    }

    private async void InitUbxDevice()
    {
        try
        {
            _state.OnNext(AsvGbsState.AsvGbsStateLoading);
            var ver = await _device.GetMonVer();
            _svc.Server.StatusText.Debug($"Found GNSS HW:{ver.Hardware}, SW:{ver.Software}, EXT:{string.Join(",", ver.Extensions)}");
            await _device.SetupByDefault();
            _state.OnNext(AsvGbsState.AsvGbsStateIdleMode);
            _isInit = true;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("Error to init GNSS");
            _svc.Server.StatusText.Error(e.Message);
            _svc.Server.StatusText.Error("Reconnect after 5 sec...");
            _state.OnNext(AsvGbsState.AsvGbsStateError);
            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => InitUbxDevice(),DisposeCancel);
        }
        
    }

    #endregion

    public async Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
    {
        if (CheckInitAndBeginCall() == false) return MavResult.MavResultTemporarilyRejected;
        try
        {
            _svc.Server.StatusText.Info($"Set GNSS AUTO mode (dur:{duration:F0},acc:{accuracy:F0})");
            await _device.SetSurveyInMode((uint)duration, accuracy, cancel);
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("GNSS AUTO mode error");
            _svc.Server.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }

   

    #region Checks

    private void EndCall()
    {
        Interlocked.Exchange(ref _busy, 0);
    }

    private bool CheckInitAndBeginCall()
    {
        // this is for reject duplicate requests
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            _svc.Server.StatusText.Warning("Temporarily rejected: now is busy");
            return false;
        }

        if (_isInit == false)
        {
            _svc.Server.StatusText.Warning("Temporarily rejected: now is busy");
            return false;
        }

        return true;
    }

    #endregion

    public async Task<MavResult> StartFixedMode(GeoPoint geoPoint,float accuracy, CancellationToken cancel)
    {
        if (CheckInitAndBeginCall() == false) return MavResult.MavResultTemporarilyRejected;

        try
        {
            _svc.Server.StatusText.Info($"Set GNSS FIXED mode ({geoPoint})");
            await _device.SetFixedBaseMode(geoPoint,accuracy,cancel);
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("GNSS FIXED mode error");
            _svc.Server.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }

    public async Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        if (CheckInitAndBeginCall() == false) return MavResult.MavResultTemporarilyRejected;
        try
        {
            await _device.RebootReceiver(cancel);
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("GNSS IDLE mode error");
            _svc.Server.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
    }

    public IRxValue<AsvGbsState> State => _state;
    public IRxValue<GeoPoint> Position => _position;
}