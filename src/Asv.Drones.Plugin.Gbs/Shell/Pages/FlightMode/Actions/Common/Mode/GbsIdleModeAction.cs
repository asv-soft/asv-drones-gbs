using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsIdleModeAction<TTarget>() : GbsModeActionBase<TTarget>("idle-mode")
    where TTarget : class, IViewModel, IDeviceActionTarget<GbsClientDevice>
{
    public const MaterialIconKind ActionIcon = MaterialIconKind.Radio;

    protected override IMenuItem? TryCreateAction(
        TTarget widget,
        CompositeDisposable contextDispose
    )
    {
        var gbs = TryGetGbsClient(widget);
        if (gbs is null)
        {
            return null;
        }

        var item = CreateMenuItem(RS.GbsIdleModeAction_Header);
        item.StaysOpenOnClick = true;
        item.Icon = ActionIcon;
        item.Description = RS.GbsIdleModeAction_Description;
        item.Order = 30;

        var canExecute = CreateModeCanExecute(
            gbs,
            mode =>
                mode
                    is AsvGbsCustomMode.AsvGbsCustomModeAuto
                        or AsvGbsCustomMode.AsvGbsCustomModeFixed,
            contextDispose
        );

        item.Command = canExecute
            .ToReactiveCommand<Unit>(
                (_, ct) =>
                    ExecuteWithErrorHandling(
                        item,
                        cancel => new ValueTask(gbs.StartIdleMode(cancel)),
                        ct
                    )
            )
            .DisposeItWith(contextDispose);

        return item;
    }
}
