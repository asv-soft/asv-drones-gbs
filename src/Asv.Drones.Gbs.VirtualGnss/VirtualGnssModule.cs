using System.ComponentModel.Composition;
using System.Reactive.Linq;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Gbs.Core;
using Asv.Drones.Gbs.Core.Vehicles;
using Asv.Mavlink;
using Asv.Mavlink.Server;
using Asv.Mavlink.V2.AsvGbs;
using Asv.Mavlink.V2.Common;
using NLog;

namespace Asv.Drones.Gbs.VirtualGnss;

public class VirtualGnssModuleConfig
{
    public bool IsEnabled { get; set; } = false;
    public int GbsStatusRateMs { get; set; } = 1000;
}

[Export(typeof(IModule))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class VirtualGnssModule: GbsClientDeviceBase, IModule,IGbsClientDevice
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IGbsMavlinkService _svc;
    private readonly VirtualGnssModuleConfig _config;
    private readonly GbsServerDevice _server;
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
            _logger.Warn("Virtual GNSS module is disbaled and will be ignored");
            return;
        }
        _random = new Random();
        _server = new GbsServerDevice(this, svc.Server, disposeServer: false)
            .DisposeItWith(Disposable);

        Observable.Timer(TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(1))
            .Subscribe(SendGbsStatus)
            .DisposeItWith(Disposable);
        
    }

    private void SendGbsStatus(long l)
    {
        InternalPosition.OnNext(new GeoPoint(36.889561, 30.718213, 1.1));
        InternalVehicleCount.OnNext((byte)_random.Next(0,3));
        InternalAccuracyMeter.OnNext(Math.Max(0,10*360 - l));
        InternalObservationSec.OnNext((ushort)l);
        InternalDgpsRate.OnNext((ushort)_random.Next(0,115200));
        
        InternalGalSatellites.OnNext((byte)_random.Next(0, 20));
        InternalBeidouSatellites.OnNext((byte)_random.Next(0, 20));
        InternalGlonassSatellites.OnNext((byte)_random.Next(0, 20));
        InternalGpsSatellites.OnNext((byte)_random.Next(0, 20));
        InternalQzssSatellites.OnNext((byte)_random.Next(0, 20));
        InternalSbasSatellites.OnNext((byte)_random.Next(0, 20));
        InternalImesSatellites.OnNext((byte)_random.Next(0, 20));
        InternalAllSatellites.OnNext((byte)(InternalGalSatellites.Value+InternalBeidouSatellites.Value+InternalGlonassSatellites.Value+InternalGpsSatellites.Value+InternalQzssSatellites.Value+InternalSbasSatellites.Value+InternalImesSatellites.Value));
    }

    public void Init()
    {
        if (_config.IsEnabled == false) return;
        _server.Server.StatusText.Info("Begin init... wait 10 sec");
        Thread.Sleep(10000);
        InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
    }

    public override Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
    {
        _server.Server.StatusText.Info("StartAutoMode... wait 5 sec");
        InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAuto));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public override Task<MavResult> StartFixedMode(GeoPoint geoPoint, float accuracy, CancellationToken cancel)
    {
        _server.Server.StatusText.Info("StartFixedMode... wait 5 sec");
        InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixedInProgress);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixed));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public override Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        _server.Server.StatusText.Info("StartIdleMode... wait 5 sec");
        InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeLoading);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => InternalCustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle));
        return Task.FromResult(MavResult.MavResultAccepted);
    }
       
}