using System.ComponentModel.Composition;
using System.Reactive.Linq;
using Asv.Cfg;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.V2.AsvGbs;
using Asv.Mavlink.V2.Common;
using NLog;

namespace Asv.Drones.Gbs;

public class VirtualGnssModuleConfig
{
#if DEBUG
    public bool IsEnabled { get; set; } = true;
#else
    public bool IsEnabled { get; set; } = false;
#endif
    public int GbsStatusRateMs { get; set; } = 1000;
}

[Export(typeof(IModule))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class VirtualGnssModule: DisposableOnceWithCancel, IModule
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly IGbsMavlinkService _svc;
    private readonly VirtualGnssModuleConfig _config;
    private IDisposable _lastActionTimer;
    private readonly Random _random;

    [ImportingConstructor]
    public VirtualGnssModule(IGbsMavlinkService svc,IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        _config = configuration.Get<VirtualGnssModuleConfig>();
        // if disabled => do nothing
        if (_config.IsEnabled == false)
        {
            Logger.Warn("Virtual GNSS module is disbaled and will be ignored");
            return;
        }
        _random = new Random();
        _svc.Server.Gbs.StartAutoMode = StartAutoMode;
        _svc.Server.Gbs.StartFixedMode = StartFixedMode;
        _svc.Server.Gbs.StartIdleMode = StartIdleMode;

        Observable.Timer(TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(1))
            .Subscribe(SendGbsStatus)
            .DisposeItWith(Disposable);
        
    }

    private void SendGbsStatus(long l)
    {
        _svc.Server.Gbs.Position.OnNext(new GeoPoint(55.2905802,61.6063891, 200.1));
        _svc.Server.Gbs.AccuracyMeter.OnNext(Math.Max(0,10*360 - l));
        _svc.Server.Gbs.ObservationSec.OnNext((ushort)l);
        _svc.Server.Gbs.DgpsRate.OnNext((ushort)_random.Next(0,115200));

        _svc.Server.Gbs.GalSatellites.OnNext((byte)_random.Next(0, 20));
        _svc.Server.Gbs.BeidouSatellites.OnNext((byte)_random.Next(0, 20));
        _svc.Server.Gbs.GlonassSatellites.OnNext((byte)_random.Next(0, 20));
        _svc.Server.Gbs.GpsSatellites.OnNext((byte)_random.Next(0, 20));
        _svc.Server.Gbs.QzssSatellites.OnNext((byte)_random.Next(0, 20));
        _svc.Server.Gbs.SbasSatellites.OnNext((byte)_random.Next(0, 20));
        _svc.Server.Gbs.ImesSatellites.OnNext((byte)_random.Next(0, 20));
        _svc.Server.Gbs.AllSatellites.OnNext((byte)(_svc.Server.Gbs.GalSatellites.Value+_svc.Server.Gbs.BeidouSatellites.Value+_svc.Server.Gbs.GlonassSatellites.Value+_svc.Server.Gbs.GpsSatellites.Value+_svc.Server.Gbs.QzssSatellites.Value+_svc.Server.Gbs.SbasSatellites.Value+_svc.Server.Gbs.ImesSatellites.Value));
    }

    public void Init()
    {
        if (_config.IsEnabled == false) return;
        _svc.Server.StatusText.Info("Begin init... wait 10 sec");
        Thread.Sleep(10000);
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
    }

    private Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
    {
        _svc.Server.StatusText.Info("StartAutoMode... wait 5 sec");
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAuto));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    private Task<MavResult> StartFixedMode(GeoPoint geoPoint, float accuracy, CancellationToken cancel)
    {
        _svc.Server.StatusText.Info("StartFixedMode... wait 5 sec");
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixedInProgress);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixed));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    private Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        _svc.Server.StatusText.Info("StartIdleMode... wait 5 sec");
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeLoading);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle));
        return Task.FromResult(MavResult.MavResultAccepted);
    }
       
}