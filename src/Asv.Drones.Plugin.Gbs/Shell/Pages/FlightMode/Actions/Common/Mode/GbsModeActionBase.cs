using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public abstract class GbsModeActionBase<TTarget>(string id) : GbsActionBase<TTarget>(id)
    where TTarget : class, IViewModel, IDeviceActionTarget<GbsClientDevice>
{
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
}
