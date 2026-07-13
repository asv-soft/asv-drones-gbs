using Asv.Drones.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class TelemetryItemFactoryRegistrations
{
    extension(TelemetrySectionRegistrations.Builder builder)
    {
        public TelemetrySectionRegistrations.Builder RegisterTelemetryItemFactories()
        {
            builder.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsAccuracyTelemetryFactory
            >();
            builder.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsModeTelemetryFactory
            >();
            builder.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsDGpsRateTelemetryFactory
            >();
            builder.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsObservationTelemetryFactory
            >();
            builder.AppBuilder.Services.AddSingleton<
                ITelemetryItemFactory,
                GbsVisibleSatellitesTelemetryFactory
            >();
            return builder;
        }
    }
}
