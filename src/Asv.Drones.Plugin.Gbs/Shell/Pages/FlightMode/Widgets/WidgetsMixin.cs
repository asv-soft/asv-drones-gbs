using Asv.Avalonia;
using Asv.Drones.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class WidgetsMixin
{
    extension(FlightModeMixin.Builder builder)
    {
        public FlightModeMixin.Builder RegisterWidgets(Action<Builder>? configure = null)
        {
            configure ??= b =>
            {
                b.RegisterDefault();
            };
            configure(new Builder(builder));
            return builder;
        }

        public Builder Widgets => new(builder);
    }

    public class Builder(FlightModeMixin.Builder builder)
    {
        public void RegisterDefault()
        {
            builder.Widgets.RegisterGbsWidget();
            builder.Widgets.RegisterSections();
        }

        public FlightModeMixin.Builder FlightMode => builder;
    }
}
