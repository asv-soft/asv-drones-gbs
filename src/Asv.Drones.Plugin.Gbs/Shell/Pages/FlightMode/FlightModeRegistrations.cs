using Asv.Avalonia;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class FlightModeRegistrations
{
    extension(PagesRegistrations.Builder builder)
    {
        public Builder FlightMode => new(builder);

        public PagesRegistrations.Builder RegisterFlightMode(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }
    }

    public class Builder(PagesRegistrations.Builder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder.AppBuilder;

        public Builder RegisterDefault()
        {
            this.RegisterAnchors();
            this.RegisterWidgets();
            return this;
        }
    }
}
