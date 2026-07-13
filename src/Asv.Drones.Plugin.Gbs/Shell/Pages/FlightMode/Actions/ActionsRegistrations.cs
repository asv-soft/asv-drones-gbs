using Asv.Avalonia;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class ActionsRegistrations
{
    extension(GbsWidgetRegistrations.Builder builder)
    {
        public Builder Actions => new(builder);

        public GbsWidgetRegistrations.Builder RegisterActions(Action<Builder>? configure = null)
        {
            configure ??= b => b.RegisterDefault();
            configure.Invoke(new Builder(builder));
            return builder;
        }
    }

    public class Builder(GbsWidgetRegistrations.Builder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder.AppBuilder;

        public Builder RegisterDefault()
        {
            this.RegisterGbsWidgetActions();
            this.RegisterGbsAnchorActions();
            this.RegisterDialogs();
            return this;
        }
    }
}
