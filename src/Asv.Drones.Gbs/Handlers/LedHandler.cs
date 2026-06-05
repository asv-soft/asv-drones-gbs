using Asv.Common;
using Asv.Drones.Gbs;
using Asv.Drones.Gbs.Led;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using R3;

namespace Asv.Drones.Rsga;

public static class LedHandlerMixin
{
    public static IHostApplicationBuilder AddLedHandler(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<LedHandler>();
        return builder;
    }
}

public class LedHandler : AsyncDisposableOnce, IHostedService
{
    private readonly ILedService _ind;
    private readonly IDisposable _disposeIt;

    public LedHandler(ILedService ind, IMavlinkService mav, ISystemControlService systemControl)
    {
        _ind = ind;
        var builder = Disposable.CreateBuilder();
        mav.Params.OnUpdated.Where(x => x.IsRemoteChange)
            .Subscribe(x => ind.LedAnimation("RG"))
            .AddTo(ref builder);
        systemControl
            .IsRebootRequested.Where(x => x)
            .Subscribe(x => ind.LedAnimation("RGRG____RGR___RG__G"))
            .AddTo(ref builder);
        _disposeIt = builder.Build();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ind.LedAnimation("RG*");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposeIt.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposeIt is IAsyncDisposable disposeItAsyncDisposable)
        {
            await disposeItAsyncDisposable.DisposeAsync();
        }
        else
        {
            _disposeIt.Dispose();
        }

        await base.DisposeAsyncCore();
    }
}
