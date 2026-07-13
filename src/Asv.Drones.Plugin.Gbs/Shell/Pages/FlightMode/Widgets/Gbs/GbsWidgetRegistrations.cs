using Asv.Avalonia;
using Asv.Drones.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsWidgetRegistrations
{
    extension(WidgetsRegistrations.Builder builder)
    {
        public Builder Gbs => new(builder);

        public WidgetsRegistrations.Builder RegisterGbsWidget(Action<Builder>? configure = null)
        {
            builder.AppBuilder.Services.AddSingleton<
                IClientDeviceWidgetCreationHandler,
                GbsFlightWidgetCreationHandler
            >();
            builder.AppBuilder.ViewLocator.RegisterViewFor<
                GbsFlightWidgetViewModel,
                FlightWidgetView
            >();
            configure ??= b => b.RegisterDefault();
            configure.Invoke(new Builder(builder));
            return builder;
        }
    }

    public class Builder(WidgetsRegistrations.Builder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder.AppBuilder;

        public Builder RegisterDefault()
        {
            this.RegisterActions();
            this.RegisterSections();
            return this;
        }
    }
}
