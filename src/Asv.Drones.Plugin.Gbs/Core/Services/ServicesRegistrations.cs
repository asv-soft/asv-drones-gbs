using Asv.Avalonia;
using Asv.Avalonia.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public static class ServicesRegistrations
{
    extension(CoreRegistrations.Builder builder)
    {
        public Builder Services => new(builder);

        public CoreRegistrations.Builder RegisterServices(Action<Builder>? configure = null)
        {
            configure ??= b => b.RegisterDefault();
            configure.Invoke(new Builder(builder));
            return builder;
        }
    }

    public class Builder(CoreRegistrations.Builder builder) : IDependencyBuilder
    {
        public IHostApplicationBuilder AppBuilder => builder.AppBuilder;

        public Builder RegisterDefault()
        {
            RegisterGbsDeviceManagerExtension();
            return this;
        }

        public Builder RegisterGbsDeviceManagerExtension()
        {
            AppBuilder.Services.AddSingleton<IDeviceManagerExtension, GbsDeviceManagerExtension>();
            return this;
        }
    }
}
