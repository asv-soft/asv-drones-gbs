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

public class GbsServerServiceConfig
{
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

    public byte ComponentId { get; set; } = 13;
    public byte SystemId { get; set; } = 13;
    public GbsServerDeviceConfig Server { get; set; } = new();
}

[Export(typeof(IGbsMavlinkService))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class GbsMavlinkService : DisposableOnceWithCancel, IGbsMavlinkService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    [ImportingConstructor]
    public GbsMavlinkService(IConfiguration config, IPacketSequenceCalculator sequenceCalculator,CompositionContainer container)
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
            }, sequenceCalculator,Scheduler.Default, cfg.Server)
            .DisposeItWith(Disposable);
        Server.Start();

        Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(_ =>
        {
            var version = Assembly.GetExecutingAssembly().GetVersion().ToString();
            Server.StatusText.Log(MavSeverity.MavSeverityInfo, $"GBS version: {version}");
        });
    }

    public IMavlinkRouter Router { get; }
    public IGbsServerDevice Server { get; }
}