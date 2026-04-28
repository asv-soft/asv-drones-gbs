using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class TelemetryMixin
{
    public static CoreMixin.Builder RegisterTelemetry(this CoreMixin.Builder builder)
    {
        builder.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            AccuracyGbsTelemetryViewModel,
            SingleRttBoxView
        >();

        builder.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            BaseStationModeGbsTelemetryViewModel,
            SingleRttBoxView
        >();

        builder.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            DGpsRateGbsTelemetryViewModel,
            SingleRttBoxView
        >();

        builder.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            LinkQualityGbsTelemetryViewModel,
            TwoColumnRttBoxView
        >();

        builder.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            ObservationGbsTelemetryViewModel,
            SingleRttBoxView
        >();

        builder.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            VisibleSatellitesGbsTelemetryViewModel,
            SingleRttBoxView
        >();

        return builder;
    }
}
