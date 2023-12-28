using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using Asv.Cfg;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.V2.Common;
using NLog;

namespace Asv.Drones.Gbs;

/// <summary>
/// Represents the configuration for the GbsServerService.
/// </summary>
public class GbsServerServiceConfig
{
    /// <summary>
    /// Gets or sets the configuration of the MAVLink ports.
    /// </summary>
    /// <value>
    /// The array of <see cref="MavlinkPortConfig"/> objects that represent the configuration of the MAVLink ports.
    /// </value>
    public MavlinkPortConfig[] Ports { get; set; } = new[]
    {
        
        
           
#if DEBUG
        new MavlinkPortConfig
        {
            ConnectionString = "tcp://127.0.0.1:7341?srv=true",
            Name = "Debug to Asv.Drones.Gui",
            IsEnabled = true
        },
        new MavlinkPortConfig
        {
            ConnectionString = "tcp://127.0.0.1:5762",
            Name = "Debug to SITL",
            IsEnabled = true
        }
#else
            new MavlinkPortConfig
            {
                ConnectionString = "serial:/dev/ttyS1?br=115200",
                Name = "Modem",
                IsEnabled = true
            },
            new MavlinkPortConfig
            {
                ConnectionString = "tcp://172.16.0.1:7341?srv=true",
                Name = "WiFi",
                IsEnabled = true
            },
#endif               
           
            
    };

    /// <summary>
    /// Gets or sets the ComponentId of the component.
    /// </summary>
    /// <value>
    /// The ComponentId is a byte property that represents the unique identifier of the component.
    /// </value>
    public byte ComponentId { get; set; } = 13;

    /// <summary>
    /// Gets or sets the identifier of the system.
    /// </summary>
    /// <value>
    /// The identifier of the system.
    /// </value>
    /// <remarks>
    /// This property represents the byte value that uniquely identifies the system.
    /// The default value is 13.
    /// </remarks>
    public byte SystemId { get; set; } = 13;

    /// <summary>
    /// Gets or sets the configuration for the server.
    /// </summary>
    /// <value>
    /// The server configuration.
    /// </value>
    public GbsServerDeviceConfig Server { get; set; } = new();
}

/// GbsMavlinkService class provides the implementation of the IGbsMavlinkService interface.
/// It is a disposable class with a constructor and two properties.
/// The class is exported with the IGbsMavlinkService interface using the MEF attribute [Export].
/// The class is instantiated with a shared CreationPolicy.
/// The class has one private static readonly Logger named Logger.
/// The constructor takes IConfiguration, IPacketSequenceCalculator, CompositionContainer,
/// and IEnumerable<IMavParamTypeMetadata> parameters.
/// It initializes the Router with the registration of default dialects, disposes it with Disposable,
/// and adds ports from the provided configuration.
/// It creates a GbsServerDevice, disposes it with Disposable, and starts the server.
/// It also sets a timer to log the GBS version after a delay of 5 seconds.
/// The class has two public properties: Router and Server which provide access to the
/// MavlinkRouter and GbsServerDevice instances respectively.
/// /
[Export(typeof(IGbsMavlinkService))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class GbsMavlinkService : DisposableOnceWithCancel, IGbsMavlinkService
{
    /// <summary>
    /// Holds an instance of logger for the current class.
    /// </summary>
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// GbsMavlinkService class represents a service that handles MAVLink communication. </summary>
    /// /
    [ImportingConstructor]
    public GbsMavlinkService(IConfiguration config, IPacketSequenceCalculator sequenceCalculator,CompositionContainer container, [ImportMany]IEnumerable<IMavParamTypeMetadata> param)
    {
        Router = new MavlinkRouter(MavlinkV2Connection.RegisterDefaultDialects).DisposeItWith(Disposable);
        var cfg = config.Get<GbsServerServiceConfig>();
        foreach (var port in cfg.Ports)
        {
            Logger.Trace($"Add port {port.Name}: {port.ConnectionString}");
            Router.AddPort(port);
        }
        Logger.Trace($"Create device SYS:{cfg.SystemId}, COM:{cfg.ComponentId}");
        Server = new GbsServerDevice(Router, new MavlinkServerIdentity
            {
                ComponentId = cfg.ComponentId,
                SystemId = cfg.SystemId,
            }, sequenceCalculator, Scheduler.Default, cfg.Server, param,
            new MavParamByteWiseEncoding(), new InMemoryConfiguration())
            .DisposeItWith(Disposable);
        Server.Start();

        Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
        {
            var version = Assembly.GetExecutingAssembly().GetVersion().ToString();
            Server.StatusText.Log(MavSeverity.MavSeverityInfo, $"GBS version: {version}");
        });
    }

    /// <summary>
    /// Gets the MAVLink router.
    /// </summary>
    /// <remarks>
    /// The MAVLink router is responsible for routing MAVLink messages between connected devices. It manages the routing table
    /// and forwards messages based on the destination device ID.
    /// </remarks>
    /// <returns>The MAVLink router.</returns>
    public IMavlinkRouter Router { get; }

    /// <summary>
    /// Gets the GbsServerDevice interface representing the server.
    /// </summary>
    /// <remarks>
    /// This property provides access to the server device instance for communication and control.
    /// </remarks>
    /// <returns>The GbsServerDevice interface representing the server.</returns>
    public IGbsServerDevice Server { get; }
}