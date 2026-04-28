namespace Asv.Drones.Plugin.Gbs;

public static class PagesMixin
{
    extension(ShellMixin.Builder builder)
    {
        public ShellMixin.Builder RegisterPages(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder Pages => new(builder);
    }

    public class Builder(ShellMixin.Builder builder)
    {
        public void RegisterDefault()
        {
            builder.Pages.RegisterDevicePages();
            builder.Pages.RegisterGbsSettings();
            builder.Pages.RegisterFlightMode();
        }

        public ShellMixin.Builder Shell => builder;
    }
}
