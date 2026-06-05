using Asv.Drones.Gbs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Gbs;

public static class MavlinkServiceMixin
{
    public static IHostApplicationBuilder AddMavlinkServer(
        this IHostApplicationBuilder builder,
        IMavParamsSource paramsSource
    )
    {
        builder
            .Services.AddSingleton(paramsSource)
            .AddSingleton<IMavlinkService, MavlinkServer>()
            .AddOptions<MavlinkServerOptions>()
            .Bind(builder.Configuration.GetSection(MavlinkServerOptions.Section));
        return builder;
    }
}
