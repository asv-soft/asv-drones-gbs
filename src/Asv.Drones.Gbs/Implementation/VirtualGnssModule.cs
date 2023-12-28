using System.ComponentModel.Composition;
using System.Reactive.Linq;
using Asv.Cfg;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.V2.AsvGbs;
using Asv.Mavlink.V2.Common;
using NLog;

namespace Asv.Drones.Gbs;

/// <summary>
/// Represents the configuration settings for a virtual GNSS module.
/// </summary>
public class VirtualGnssModuleConfig
{
#if DEBUG
    /// <summary>
    /// Gets or sets a value indicating whether the property is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> if the property is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsEnabled { get; set; } = true;
#else
    public bool IsEnabled { get; set; } = false;
#endif
    /// <summary>
    /// Gets or sets the rate in milliseconds at which the GbsStatus is updated.
    /// </summary>
    /// <value>
    /// The rate in milliseconds at which the GbsStatus is updated. The default value is 1000 milliseconds.
    /// </value>
    public int GbsStatusRateMs { get; set; } = 1000;
}

/// <summary>
/// Represents a virtual GNSS module that generates mock GNSS data.
/// </summary>
[Export(typeof(IModule))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class VirtualGnssModule: DisposableOnceWithCancel, IModule
{
    /// <summary>
    /// A static readonly variable that holds an instance of the Logger class.
    /// The Logger class is obtained from the LogManager class using the GetCurrentClassLogger method.
    /// </summary>
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Represents an instance of the GbsMavlinkService interface.
    /// </summary>
    private readonly IGbsMavlinkService _svc;

    /// <summary>
    /// Holds the configuration settings for the VirtualGnssModule.
    /// </summary>
    private readonly VirtualGnssModuleConfig _config;

    /// <summary>
    /// Represents a timer for tracking the time since the last action occurred.
    /// </summary>
    /// <remarks>
    /// The <see cref="_lastActionTimer"/> provides a mechanism to measure the elapsed time since the last action. It is typicaly used in scenarios where you want to trigger certain actions
    /// after a specific duration of inactivity.
    /// </remarks>
    private IDisposable _lastActionTimer;

    /// <summary>
    /// Represents a random number generator.
    /// </summary>
    private readonly Random _random;

    /// <summary>
    /// Represents a virtual GNSS module.
    /// </summary>
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

    /// <summary>
    /// Sends Gbs status to the server with specified data.
    /// </summary>
    /// <param name="l">The value used for calculating accuracy meter and observation sec. Should be a positive integer.</param>
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

    /// <summary>
    /// Initializes the object.
    /// </summary>
    public void Init()
    {
        if (_config.IsEnabled == false) return;
        _svc.Server.StatusText.Info("Begin init... wait 10 sec");
        Thread.Sleep(10000);
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
    }

    /// <summary>
    /// Starts the auto mode.
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <param name="accuracy">The accuracy.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The task representing the operation.</returns>
    private Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
    {
        _svc.Server.StatusText.Info("StartAutoMode... wait 5 sec");
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeAuto));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    /// <summary>
    /// Starts the fixed mode.
    /// </summary>
    /// <param name="geoPoint">The geographical point.</param>
    /// <param name="accuracy">The accuracy.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The task representing the asynchronous operation with a result of MavResult.</returns>
    private Task<MavResult> StartFixedMode(GeoPoint geoPoint, float accuracy, CancellationToken cancel)
    {
        _svc.Server.StatusText.Info("StartFixedMode... wait 5 sec");
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixedInProgress);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixed));
        return Task.FromResult(MavResult.MavResultAccepted);
    }

    /// <summary>
    /// Starts idle mode and waits for 5 seconds.
    /// </summary>
    /// <param name="cancel">The cancellation token to stop the idle mode.</param>
    /// <returns>A task representing the completion of the idle mode operation.</returns>
    private Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        _svc.Server.StatusText.Info("StartIdleMode... wait 5 sec");
        _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeLoading);
        _lastActionTimer?.Dispose();
        _lastActionTimer = Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ => _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle));
        return Task.FromResult(MavResult.MavResultAccepted);
    }
       
}