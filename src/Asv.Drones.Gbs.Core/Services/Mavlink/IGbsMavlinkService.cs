using Asv.Mavlink;
using Asv.Mavlink.Server;

namespace Asv.Drones.Gbs.Core;



public interface IGbsMavlinkService
{
    IMavlinkServer Server { get; }
    IMavlinkRouter Router { get; }
}