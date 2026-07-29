using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsCancelModeAction<TTarget>() : GbsModeActionBase<TTarget>("cancel-mode")
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

        var item = CreateMenuItem(RS.GbsCancelModeAction_Header);
        item.StaysOpenOnClick = true;
        item.Icon = ActionIcon;
        item.Description = RS.GbsCancelModeAction_Description;
        item.Order = 40;

        var canExecute = CreateModeCanExecute(
            gbs,
            mode =>
                mode
                    is AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress
                        or AsvGbsCustomMode.AsvGbsCustomModeFixedInProgress,
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
