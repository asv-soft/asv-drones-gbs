using Asv.Avalonia;
using Asv.Drones.Api;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsWidgetActionsRegistrations
{
    extension(ActionsRegistrations.Builder builder)
    {
        public ActionsRegistrations.Builder RegisterGbsWidgetActions()
        {
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsAutoModeAction<IGbsFlightWidget>
            >();
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsFixedModeAction<IGbsFlightWidget>
            >();
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsIdleModeAction<IGbsFlightWidget>
            >();
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsCancelModeAction<IGbsFlightWidget>
            >();
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsLocateBaseStationAction<IGbsFlightWidget>
            >();
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                ConfigureTelemetryAction<IGbsFlightWidget>
            >();
            return builder;
        }
    }
}
