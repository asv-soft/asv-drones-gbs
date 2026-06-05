using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Gbs;

public static class SystemControlServiceMixin
{
    public static IHostApplicationBuilder AddSystemControl(this IHostApplicationBuilder builder)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.Services.AddSingleton<ISystemControlService>(new SystemControlServiceWindows());
        }
        else if (
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
        )
        {
            builder.Services.AddSingleton<ISystemControlService>(new SystemControlServiceUnix());
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        return builder;
    }
}
