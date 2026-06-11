using Asv.Avalonia;
using Asv.Avalonia.InfoMessage;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public abstract class GbsFlightWidgetActionBase<TWidget>(string id)
    : FlightWidgetAction<TWidget>(id)
    where TWidget : class, IGbsFlightWidget<GbsClientDevice>
{
    protected static IAsvGbsExClient? TryGetGbsClient(TWidget widget) =>
        widget.Device.GetMicroservice<IAsvGbsExClient>();

    protected static ReactiveProperty<bool> CreateModeCanExecute(
        IAsvGbsExClient gbs,
        Func<AsvGbsCustomMode, bool> predicate,
        CompositeDisposable contextDispose
    )
    {
        var canExecute = new ReactiveProperty<bool>(
            predicate(gbs.CustomMode.CurrentValue)
        ).DisposeItWith(contextDispose);

        gbs.CustomMode.ObserveOnUIThreadDispatcher()
            .Subscribe(mode => canExecute.OnNext(predicate(mode)))
            .DisposeItWith(contextDispose);

        return canExecute;
    }

    protected static async ValueTask ExecuteWithErrorHandling(
        IViewModel owner,
        Func<CancellationToken, ValueTask> execute,
        CancellationToken cancel
    )
    {
        try
        {
            await execute(cancel);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await owner.RiseShellErrorMessage("GBS action", ex.Message, ex, cancel);
        }
    }
}
