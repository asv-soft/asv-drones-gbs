using Asv.Mavlink;

namespace Asv.Drones.Gbs;

public interface IGbsMavlinkService
{
    IMavlinkRouter Router { get; }
    IGbsServerDevice Server { get; }
}