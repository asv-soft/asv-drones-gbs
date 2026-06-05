using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.Common;
using Asv.Mavlink.Diagnostic.Server;

namespace Asv.Drones.Gbs;

public interface IMavlinkService
{
    MavlinkIdentity Identity { get; }
    IProtocolRouter Router { get; }
    IStatusTextServer StatusText { get; }
    IHeartbeatServer Heartbeat { get; }
    ICommandServerEx<CommandLongPacket> CommandLongEx { get; }
    IParamsServerEx Params { get; }
    IDiagnosticServer Diagnostic { get; }

    IAsvGbsServerEx Gbs { get; }
}
