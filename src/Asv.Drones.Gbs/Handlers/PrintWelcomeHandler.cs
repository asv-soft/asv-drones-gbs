using System.Reflection;
using Asv.Common;
using Asv.IO;
using Asv.Mavlink;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Gbs;

public static class PrintWelcomeHandlerMixin
{
    public static IHostApplicationBuilder AddPrintWelcomeHandler(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddHostedService<PrintWelcomeHandler>();
        return builder;
    }
}

public class PrintWelcomeHandler(IMavlinkService mavlink, TimeProvider timeProvider)
    : AsyncDisposableWithCancel,
        IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // ReSharper disable once MethodSupportsCancellation
        Task.Factory.StartNew(PrintInformation, DisposeCancel);
        return Task.CompletedTask;
    }

    private async void PrintInformation(object? obj)
    {
        var messageDelay = TimeSpan.FromMilliseconds(200);
        await Task.Delay(TimeSpan.FromSeconds(10), timeProvider, DisposeCancel);
        var asm = Assembly.GetExecutingAssembly();
        mavlink.StatusText.Info($"{asm.GetTitle()}");
        await Task.Delay(messageDelay);
        mavlink.StatusText.Info($"{asm.GetDescription()}");
        await Task.Delay(messageDelay);
        mavlink.StatusText.Info($"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
        await Task.Delay(messageDelay);
#if DEBUG
        mavlink.StatusText.Info($"Build : Debug");
#else
        mavlink.StatusText.Info($"Build : Release");
#endif
        mavlink.StatusText.Info($"OS : {Environment.OSVersion}");
        mavlink.StatusText.Info($"Environment : {Environment.Version}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
