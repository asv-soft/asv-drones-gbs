using Asv.Avalonia.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class ServicesMixin
{
    public static CoreMixin.Builder RegisterServices(this CoreMixin.Builder builder)
    {
        builder.GbsPlugin.AppBuilder.Services.AddSingleton<
            IDeviceManagerExtension,
            GbsDeviceManagerExtension
        >();

        return builder;
    }
}
