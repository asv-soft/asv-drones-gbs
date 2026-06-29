using Asv.Avalonia;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsIdleModeAction<TWidget>() : GbsFlightWidgetActionBase<TWidget>("idle-mode")
    where TWidget : class, IGbsFlightWidget<GbsClientDevice>
{
    public const MaterialIconKind ActionIcon = MaterialIconKind.Radio;

    protected override IMenuItem? TryCreateAction(
        TWidget widget,
        CompositeDisposable contextDispose
    )
    {
        var gbs = TryGetGbsClient(widget);
        if (gbs is null)
        {
            return null;
        }

        var item = CreateMenuItem("Disable RTK");
        item.Icon = ActionIcon;
        item.Description = "Switch GBS to idle mode";
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
