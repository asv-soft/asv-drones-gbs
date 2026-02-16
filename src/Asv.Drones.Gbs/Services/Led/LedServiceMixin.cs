using Asv.Drones.Gbs.Led;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Gbs;

public static class LedServiceMixin
{
    public static IHostApplicationBuilder AddLedService(this IHostApplicationBuilder builder)
    {
        builder
            .Services.AddSingleton<ILedService, LedService>()
            .AddOptions<LedServiceOptions>()
            .Bind(builder.Configuration.GetSection(LedServiceOptions.Section));
        return builder;
    }
}
