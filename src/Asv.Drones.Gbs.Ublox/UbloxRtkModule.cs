using System.ComponentModel.Composition;
using System.Reactive.Linq;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Gbs.Core;
using Asv.Drones.Gbs.Core.Vehicles;
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

    public ushort[] RtcmV3MessagesIdsToSend { get; set; } = {
        1005 , 1006 , 1074 , 1077 ,
        1084 , 1087 , 1094 , 1097 ,
        1124 , 1127 , 1230 , 4072
    };

    public byte MessageRateHz { get; set; } = 1;
    public int ReconnectTimeoutMs { get; set; } = 10_000;
}

[Export(typeof(IModule))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class UbloxRtkModule:GbsClientDeviceBase, IModule, IGbsClientDevice
{
    private readonly IGbsMavlinkService _svc;
    private readonly IVehicleServices _vehicle;
    private readonly UbloxRtkModuleConfig _config;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly UbxDevice _device;
    private int _busy;
    private bool _isInit;
    private int _updateStatusInProgress;
    private readonly RxValue<UbxNavSvin> _svIn;
    private readonly HashSet<ushort> _rtcmV3Filter;
    private int _sendRtcmFlag;
    private readonly GbsServerDevice _server;
    private readonly IncrementalRateCounter _rxByteRate = new(3);
    private long _rxBytes;

    [ImportingConstructor]
    public UbloxRtkModule(IGbsMavlinkService svc,IConfiguration configuration, IVehicleServices vehicle)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        _vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
        _config = configuration.Get<UbloxRtkModuleConfig>();
        // if disabled => do nothing
        if (_config.IsEnabled == false)
        {
            _logger.Warn("UBlox RTK module is disbaled and will be ignored");
            return;
        }
        
        _rtcmV3Filter = new HashSet<ushort>(_config.RtcmV3MessagesIdsToSend);
        _svIn = new RxValue<UbxNavSvin>().DisposeItWith(Disposable);
        // start to init device
        _svc.Server.StatusText.Info("Connecting to GNSS device...");
        _device = new UbxDevice(_config.ConnectionString).DisposeItWith(Disposable);
        
        _device.Connection.Filter<UbxNavSvin>().Subscribe(_svIn).DisposeItWith(Disposable);
        _device.Connection.GetRtcmV3RawMessages().Where(_=>_rtcmV3Filter.Contains(_.MessageId)).Subscribe(SendRtcm)
            .DisposeItWith(Disposable);
        
        _server = new GbsServerDevice(this, svc.Server, disposeServer: false)
            .DisposeItWith(Disposable);

        Observable.Timer(TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs), TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs))
            .Subscribe(UpdateStatus)
            .DisposeItWith(Disposable);
        
        InternalVehicleCount.OnNext((byte)_vehicle.Vehicles.Length);
        _vehicle.OnVehicleCountChanged.Subscribe(InternalVehicleCount).DisposeItWith(Disposable);
    }

    private void SendRtcm(RtcmV3RawMessage msg)
    {
        if (_isInit == false) return;
        if (InternalCustomMode.Value != AsvGbsCustomMode.AsvGbsCustomModeAuto && InternalCustomMode.Value != AsvGbsCustomMode.AsvGbsCustomModeFixed) return;
        
        if (Interlocked.CompareExchange(ref _sendRtcmFlag, 1, 0) != 0)
        {
            return;
        }
        try
        {
            Interlocked.Add(ref _rxBytes, msg.RawData.Length);
            var rate = _rxByteRate.Calculate(_rxBytes);
            InternalDgpsRate.OnNext((ushort)rate);
            _vehicle.SendRtcmData(msg.RawData, msg.RawData.Length, CancellationToken.None).Wait();
        }
        finally
        {
            Interlocked.Exchange(ref _sendRtcmFlag, 0);
        }
    }

    private async void UpdateStatus(long l)
    {
        if (Interlocked.CompareExchange(ref _updateStatusInProgress,1,0) !=0) return;
        try
        {
            if (_isInit == false) return;
            
            var cfgTMode3 = await _device.GetCfgTMode3(DisposeCancel);
            switch (cfgTMode3.Mode)
            {
                case TMode3Enum.Disabled:
                    InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
                    break;
                case TMode3Enum.SurveyIn:
                    InternalCustomMode.OnNext(_svIn.Value.Active
                        ? AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress
                        : AsvGbsCustomMode.AsvGbsCustomModeAuto);

                    InternalPosition.OnNext(_svIn.Value.Location ?? GeoPoint.Zero);
                    break;
                case TMode3Enum.FixedMode:
                    InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixed);
                    InternalPosition.OnNext(cfgTMode3.Location ?? GeoPoint.Zero);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            InternalAccuracyMeter.OnNext(_svIn.Value.Accuracy);
            InternalObservationSec.OnNext((ushort)_svIn.Value.Observations);
            
            var navSat = await _device.GetNavSat(DisposeCancel);
            InternalAllSatellites.OnNext(navSat.NumSvs);
            byte gps = 0;
            byte sbas = 0;
            byte galileo= 0;
            byte beidou= 0;
            byte imes = 0;
            byte qzss = 0;
            byte glo= 0;
            foreach (var satItem in navSat.Items)
            {
                switch (satItem.GnssType)
                {
                    case UbxNavSatGnssId.GPS:
                        ++gps;
                        break;
                    case UbxNavSatGnssId.SBAS:
                        ++sbas;
                        break;
                    case UbxNavSatGnssId.Galileo:
                        ++galileo;
                        break;
                    case UbxNavSatGnssId.BeiDou:
                        ++beidou;
                        break;
                    case UbxNavSatGnssId.IMES:
                        ++imes;
                        break;
                    case UbxNavSatGnssId.QZSS:
                        ++qzss;
                        break;
                    case UbxNavSatGnssId.GLONASS:
                        ++glo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            InternalGpsSatellites.OnNext(gps);
            InternalSbasSatellites.OnNext(sbas);
            InternalGalSatellites.OnNext(galileo);
            InternalBeidouSatellites.OnNext(beidou);
            InternalImesSatellites.OnNext(imes);
            InternalQzssSatellites.OnNext(qzss);
            InternalGlonassSatellites.OnNext(glo);
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
            InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeLoading);
            var ver = await _device.GetMonVer();
            _svc.Server.StatusText.Debug($"Found GNSS HW:{ver.Hardware}, SW:{ver.Software}, EXT:{string.Join(",", ver.Extensions)}");
            await _device.SetStationaryMode(false, _config.MessageRateHz);
            await _device.TurnOffNmea(CancellationToken.None);
            // surveyin msg - for feedback
            await _device.SetMessageRate<UbxNavSvin>(_config.MessageRateHz);
            // pvt msg - for feedback
            await _device.SetMessageRate<UbxNavPvt>(_config.MessageRateHz);
            // 1005 - 5s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RTCM3, 0x05, 5);
            
            await _device.SetupRtcmMSM4Rate(_config.MessageRateHz,DisposeCancel);
            await _device.SetupRtcmMSM7Rate(0,default);
            
            // 1230 - 5s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RTCM3, 0xE6, 5);
            
            // NAV-VELNED - 1s
            
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.NAV, 0x12, _config.MessageRateHz);
            
            // rxm-raw/rawx - 1s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x15, _config.MessageRateHz);
            //await SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x10, 1, cancel);
            
            // rxm-sfrb/sfrb - 2s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x13, 2, default);
            //await SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x11, 2, cancel);
            
            // mon-hw - 2s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.MON, 0x09, 2, default);
            InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
            _isInit = true;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("Error to init GNSS");
            _svc.Server.StatusText.Debug(e.Message);
            _svc.Server.StatusText.Debug($"Reconnect after {TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs).TotalSeconds:F0} sec...");
            InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeError);
            Observable.Timer(TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs)).Subscribe(_ => InitUbxDevice(),DisposeCancel);
        }
        
    }

    #endregion

    public override async Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
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

    public override async Task<MavResult> StartFixedMode(GeoPoint geoPoint,float accuracy, CancellationToken cancel)
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

    public override async Task<MavResult> StartIdleMode(CancellationToken cancel)
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
}