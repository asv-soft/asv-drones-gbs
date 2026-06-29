using Asv.Avalonia;
using Asv.Drones.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class SectionsMixin
{
    extension(WidgetsMixin.Builder builder)
    {
        public WidgetsMixin.Builder RegisterSections(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder Sections => new(builder);
    }

    public class Builder(WidgetsMixin.Builder builder)
    {
        public void RegisterDefault()
        {
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
                IGbsSatelliteCountSection,
                GbsSatelliteCountSectionView
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.ViewModel.RegisterWithArgs<
                IGbsSatelliteCountSection,
                GbsSatelliteCountSectionViewModel,
                GbsSatelliteCountSectionArgs
            >();

            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsAccuracyTelemetryFactory
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsModeTelemetryFactory
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsDGpsRateTelemetryFactory
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsObservationTelemetryFactory
            >();
            builder.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsVisibleSatellitesTelemetryFactory
            >();
        }

        public WidgetsMixin.Builder Widgets => builder;
    }
}
