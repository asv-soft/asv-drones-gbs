using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.Minimal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Gbs;

public static class WorkModeHandlerMixin
{
    public static IHostApplicationBuilder AddMavlinkWorkModeHandler(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddHostedService<WorkModeHandler>();
        return builder;
    }
}

public class WorkModeHandler : AsyncDisposableWithCancel, IHostedService
{
    private readonly IMavlinkService _mavlink;

    public WorkModeHandler(IMavlinkService mavlink, ILoggerFactory loggerFactory)
    {
        _mavlink = mavlink;
        mavlink.Heartbeat.Set(hb =>
        {
            hb.Autopilot = MavAutopilot.MavAutopilotInvalid;
            hb.Type = (Mavlink.Minimal.MavType)Mavlink.AsvGbs.MavType.MavTypeAsvGbs;
            hb.SystemStatus = MavState.MavStateActive;
            hb.BaseMode = MavModeFlag.MavModeFlagCustomModeEnabled;
            hb.MavlinkVersion = 3;
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _mavlink.Heartbeat.Start();
        _mavlink.Gbs.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
