using Material.Icons;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsPluginMixin
{
    public const MaterialIconKind DefaultIcon = MaterialIconKind.Radio;

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder UseGbsPlugin(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder GbsPlugin => new(builder);
    }

    public class Builder(IHostApplicationBuilder builder)
    {
        public void RegisterDefault()
        {
            builder.GbsPlugin.RegisterCore();
            builder.GbsPlugin.RegisterShell();
        }

        public IHostApplicationBuilder AppBuilder => builder;
    }
}
