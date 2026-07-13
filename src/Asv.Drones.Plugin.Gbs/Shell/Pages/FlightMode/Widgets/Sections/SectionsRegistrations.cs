using Asv.Avalonia;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class SectionsRegistrations
{
    extension(GbsWidgetRegistrations.Builder builder)
    {
        public Builder Sections => new(builder);

        public GbsWidgetRegistrations.Builder RegisterSections(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }
    }

    public class Builder(GbsWidgetRegistrations.Builder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder.AppBuilder;

        public Builder RegisterDefault()
        {
            this.RegisterTelemetrySection();
            this.RegisterSatelliteCountSection();
            return this;
        }
    }
}
