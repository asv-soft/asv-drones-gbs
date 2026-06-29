using Asv.Avalonia;
using Asv.Drones.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsWidgetMixin
{
    extension(WidgetsMixin.Builder builder)
    {
        public WidgetsMixin.Builder RegisterGbsWidget(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder Gbs => new(builder);
    }

    public class Builder(WidgetsMixin.Builder builder)
    {
        public void RegisterDefault()
        {
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Services.AddSingleton<
                IClientDeviceWidgetCreationHandler,
                GbsFlightWidgetCreationHandler
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
                GbsFlightWidgetViewModel,
                FlightWidgetView
            >();

            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsFlightWidgetTelemetrySectionExtension
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsFlightWidgetSatelliteCountSectionExtension
            >();

            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsAutoModeAction<IGbsFlightWidget>
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsFixedModeAction<IGbsFlightWidget>
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsIdleModeAction<IGbsFlightWidget>
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsCancelModeAction<IGbsFlightWidget>
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsLocateBaseStationAction<IGbsFlightWidget>
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                ConfigureTelemetryAction<IGbsFlightWidget>
            >();

            builder.Gbs.RegisterDialogs();
        }

        public WidgetsMixin.Builder Widgets => builder;
    }
}
