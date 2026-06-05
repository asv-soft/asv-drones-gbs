using System.Diagnostics;
using Asv.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.Drones.Gbs;

public static class ExceptionHandlerMixin
{
    public static IHostApplicationBuilder AddExceptionHandler(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<ExceptionHandler>();
        return builder;
    }
}

public class ExceptionHandler : IHostedService
{
    public ExceptionHandler(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(ExceptionHandler));
        AsyncTaskExtensions.SetDefaultExceptionHandler(x =>
            logger.ZLogWarning(x, $"Error to execute task: {x.Message}")
        );
        ObservableSystem.RegisterUnhandledExceptionHandler(ex =>
        {
            {
                logger.ZLogCritical(ex, $"R3 unobserved exception: {ex.Message}");
            }
        });
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            logger.ZLogCritical(
                args.Exception,
                $"Task scheduler unobserved task exception from '{sender}': {args.Exception.Message}"
            );
            Debug.Fail($"Task scheduler unobserved exception: {args.Exception.Message}");
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            logger.ZLogCritical(
                $"Unhandled AppDomain exception. Sender '{sender}'. Args: {eventArgs.ExceptionObject}"
            );
        };
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
