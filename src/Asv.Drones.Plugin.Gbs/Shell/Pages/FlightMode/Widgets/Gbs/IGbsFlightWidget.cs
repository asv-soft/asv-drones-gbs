using Asv.Drones.Api;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public interface IGbsFlightWidget : IGbsFlightWidget<GbsClientDevice> { }

public interface IGbsFlightWidget<TGbs> : IMavlinkDeviceFlightWidget<TGbs>
    where TGbs : GbsClientDevice { }
