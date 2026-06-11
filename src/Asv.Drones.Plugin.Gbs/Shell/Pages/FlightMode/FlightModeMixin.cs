namespace Asv.Drones.Plugin.Gbs;

public static class FlightModeMixin
{
    extension(PagesMixin.Builder builder)
    {
        public PagesMixin.Builder RegisterFlightMode(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder FlightMode => new(builder);
    }

    public class Builder(PagesMixin.Builder builder)
    {
        public void RegisterDefault()
        {
            builder.FlightMode.RegisterAnchors();
            builder.FlightMode.RegisterWidgets();
        }

        public PagesMixin.Builder Pages => builder;
    }
}
