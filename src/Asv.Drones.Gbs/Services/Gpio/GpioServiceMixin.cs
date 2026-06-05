using Asv.Hal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Gbs.Gpio;

public static class GpioServiceMixin
{
    public static IHostApplicationBuilder AddDefaultGpioService(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddSingleton<IGpioProvider, LibGpioProvider>();
        return builder;
    }
}
