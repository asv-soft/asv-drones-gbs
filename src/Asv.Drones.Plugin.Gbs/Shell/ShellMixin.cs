using Asv.Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class ShellMixin
{
    extension(GbsPluginMixin.Builder builder)
    {
        public GbsPluginMixin.Builder RegisterShell(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder Shell => new(builder);
    }

    public class Builder(GbsPluginMixin.Builder builder)
    {
        public void RegisterDefault()
        {
            builder.Shell.RegisterPages();
        }

        public GbsPluginMixin.Builder GbsPlugin => builder;
    }
}
