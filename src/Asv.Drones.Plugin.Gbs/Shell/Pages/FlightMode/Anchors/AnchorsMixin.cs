using Asv.Avalonia;
using Asv.Drones.Api;

namespace Asv.Drones.Plugin.Gbs;

public static class AnchorsMixin
{
    public static FlightModeMixin.Builder RegisterAnchors(this FlightModeMixin.Builder builder)
    {
        builder.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
            IFlightModePage,
            FlightModeAnchorsExtension
        >();

        return builder;
    }
}
