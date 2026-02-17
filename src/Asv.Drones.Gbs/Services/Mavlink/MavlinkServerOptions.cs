using Asv.Mavlink;
using Asv.Mavlink.Diagnostic.Server;

namespace Asv.Drones.Gbs;

public class MavlinkServerOptions
{
    public const string Section = "Mavlink";
    public string[] Connections { get; set; } = [];
    public MavlinkHeartbeatServerConfig Heartbeat { get; set; } = new();
    public StatusTextLoggerConfig StatusText { get; set; } = new();
    public ParamsServerExConfig Params { get; set; } = new();
    public DiagnosticServerConfig Diagnostic { get; set; } = new();
    public AsvChartServerConfig Charts { get; set; } = new();
    public AsvGbsServerConfig Gbs { get; set; } = new();
}
