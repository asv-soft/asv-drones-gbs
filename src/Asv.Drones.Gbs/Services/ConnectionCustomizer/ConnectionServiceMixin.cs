using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Gbs;

public static class ConnectionServiceMixin
{
    public static IHostApplicationBuilder AddUBloxConnectionService(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddSingleton<IDeviceConnectionsService, UBloxDeviceConnectionsService>();
        return builder;
    }
}
