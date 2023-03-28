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
public class VirtualGnssModule: DisposableOnceWithCancel, IModule,IGbsClientDevice
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IGbsMavlinkService _svc;
    private readonly VirtualGnssModuleConfig _config;
    private readonly RxValue<AsvGbsCustomMode> _mode;
    private readonly RxValue<GeoPoint> _position;
    private readonly GbsServerDevice _server;

    [ImportingConstructor]
    public VirtualGnssModule(IGbsMavlinkService svc,IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        _config = configuration.Get<VirtualGnssModuleConfig>();
        _mode = new RxValue<AsvGbsCustomMode>(AsvGbsCustomMode.AsvGbsCustomModeLoading).DisposeItWith(Disposable);
        _position = new RxValue<GeoPoint>(GeoPoint.Zero).DisposeItWith(Disposable);
        // if disabled => do nothing
        if (_config.IsEnabled == false)
        {
            _logger.Warn("Virtual GNSS module is disbaled and will be ignored");
            return;
        }

        _server = new GbsServerDevice(this, svc.Server, disposeServer: false)
            .DisposeItWith(Disposable);

    }

    public void Init()
    {
        if (_config.IsEnabled == false) return;
        Thread.Sleep(10000);
        _mode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
    }

    public Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
    {
        _mode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress);
        Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ => _mode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAuto));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public Task<MavResult> StartFixedMode(GeoPoint geoPoint, float accuracy, CancellationToken cancel)
    {
        _mode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixedInProgress);
        Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ => _mode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixed));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        _mode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeLoading);
        Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ => _mode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public IRxValue<AsvGbsCustomMode> CustomMode => _mode;
    public IRxValue<GeoPoint> Position => _position;
    
}