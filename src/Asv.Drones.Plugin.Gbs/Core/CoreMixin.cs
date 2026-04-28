namespace Asv.Drones.Plugin.Gbs;

public static class CoreMixin
{
    extension(GbsPluginMixin.Builder builder)
    {
        public GbsPluginMixin.Builder RegisterCore(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder Core => new(builder);
    }

    public class Builder(GbsPluginMixin.Builder builder)
    {
        public void RegisterDefault()
        {
            builder.Core.RegisterCommands();
            builder.Core.RegisterServices();
        }

        public GbsPluginMixin.Builder GbsPlugin => builder;
    }
}
