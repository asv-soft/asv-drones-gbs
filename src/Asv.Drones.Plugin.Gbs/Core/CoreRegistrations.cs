using Asv.Avalonia;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class CoreRegistrations
{
    extension(GbsPluginRegistrations.Builder builder)
    {
        public Builder Core => new(builder);

        public GbsPluginRegistrations.Builder RegisterCore(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }
    }

    public class Builder(GbsPluginRegistrations.Builder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder.AppBuilder;

        public Builder RegisterDefault()
        {
            this.RegisterServices();
            return this;
        }
    }
}
