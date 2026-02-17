using Asv.Common;
using Asv.Drones.Gbs;
using Asv.Drones.Gbs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Rsga;

public static class SystemControlMixin
{
    public static IHostApplicationBuilder AddSystemControlHandler(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddHostedService<SystemControlHandler>();
        return builder;
    }
}

public class SystemControlHandler : AsyncDisposableWithCancel, IHostedService
{
    public SystemControlHandler(
        IMavlinkService mavlink,
        ISystemControlService svc,
        ILoggerFactory loggerFactory
    )
    {
        var logger = loggerFactory.CreateLogger<SystemControlHandler>();
        mavlink.Params.OnInt32Command(
            MavParams.BrdRestartCmd,
            DisposeCancel,
            logger,
            cancel => svc.Do(SystemControlAction.Restart)
        );
        mavlink.Params.OnInt32Command(
            MavParams.BrdRebootCmd,
            DisposeCancel,
            logger,
            cancel => svc.Do(SystemControlAction.Reboot)
        );
        mavlink.Params.OnInt32Command(
            MavParams.BrdShutdownCmd,
            DisposeCancel,
            logger,
            cancel => svc.Do(SystemControlAction.Shutdown)
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
