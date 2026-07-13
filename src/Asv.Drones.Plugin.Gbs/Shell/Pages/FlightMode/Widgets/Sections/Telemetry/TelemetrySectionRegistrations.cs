using Asv.Avalonia;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class TelemetrySectionRegistrations
{
    extension(SectionsRegistrations.Builder builder)
    {
        public Builder Telemetry => new(builder);

        public SectionsRegistrations.Builder RegisterTelemetrySection(
            Action<Builder>? configure = null
        )
        {
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsFlightWidgetTelemetrySectionExtension
            >();
            configure ??= b => b.RegisterDefault();
            configure.Invoke(new Builder(builder));
            return builder;
        }
    }

    public class Builder(SectionsRegistrations.Builder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder.AppBuilder;

        public Builder RegisterDefault()
        {
            this.RegisterTelemetryItemFactories();
            return this;
        }
    }
}
