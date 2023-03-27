using System.ComponentModel.Composition;
using System.Reactive.Linq;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Gbs.Core;
using Asv.Drones.Gbs.Core.Vehicles;
using Asv.Mavlink;
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
public class VirtualGnssModule: DisposableOnceWithCancel, IModule,IAsvGbsClient
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IGbsMavlinkService _svc;
    private readonly VirtualGnssModuleConfig _config;
    private readonly RxValue<AsvGbsState> _state;
    private readonly RxValue<GeoPoint> _position;

    [ImportingConstructor]
    public VirtualGnssModule(IGbsMavlinkService svc,IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        _config = configuration.Get<VirtualGnssModuleConfig>();
        _state = new RxValue<AsvGbsState>(AsvGbsState.AsvGbsStateLoading).DisposeItWith(Disposable);
        _position = new RxValue<GeoPoint>(GeoPoint.Zero).DisposeItWith(Disposable);
        // if disabled => do nothing
        if (_config.IsEnabled == false)
        {
            _logger.Warn("Virtual GNSS module is disbaled and will be ignored");
            return;
        }
        
        _svc.UpdateCustomMode(_=>_.Compatibility |= AsvGbsCompatibility.RtkMode );
        _svc.Server.Gbs.Init(TimeSpan.FromMilliseconds(_config.GbsStatusRateMs),this );
        
    }

    public void Init()
    {
        if (_config.IsEnabled == false) return;
        Thread.Sleep(10000);
        _state.OnNext(AsvGbsState.AsvGbsStateIdleMode);
    }

    public Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
    {
        _state.OnNext(AsvGbsState.AsvGbsStateAutoModeInProgress);
        Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ => _state.OnNext(AsvGbsState.AsvGbsStateAutoMode));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public Task<MavResult> StartFixedMode(GeoPoint geoPoint, float accuracy, CancellationToken cancel)
    {
        _state.OnNext(AsvGbsState.AsvGbsStateFixedModeInProgress);
        Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ => _state.OnNext(AsvGbsState.AsvGbsStateFixedMode));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        _state.OnNext(AsvGbsState.AsvGbsStateLoading);
        Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(_ => _state.OnNext(AsvGbsState.AsvGbsStateIdleMode));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    public IRxValue<AsvGbsState> State => _state;
    public IRxValue<GeoPoint> Position => _position;
}