using Asv.Avalonia;
using Asv.Drones.Api;

namespace Asv.Drones.Plugin.Gbs;

public static class FlightModeAnchorRegistrations
{
    public static FlightModeRegistrations.Builder RegisterAnchors(
        this FlightModeRegistrations.Builder builder
    )
    {
        builder.AppBuilder.Extensions.Register<IFlightModePage, FlightModeAnchorsExtension>();

        return builder;
    }
}
