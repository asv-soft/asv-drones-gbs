using Asv.Avalonia;
using Material.Icons;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsPluginRegistrations
{
    public const MaterialIconKind DefaultIcon = MaterialIconKind.Radio;

    extension(IHostApplicationBuilder builder)
    {
        public Builder GbsPlugin => new(builder);

        public IHostApplicationBuilder RegisterGbsPlugin(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }
    }

    public class Builder(IHostApplicationBuilder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder;

        public Builder RegisterDefault()
        {
            this.RegisterCore();
            this.RegisterShell();
            return this;
        }
    }
}
