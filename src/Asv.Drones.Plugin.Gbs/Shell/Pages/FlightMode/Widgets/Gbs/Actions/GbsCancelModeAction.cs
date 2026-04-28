using Asv.Avalonia;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsCancelModeAction<TWidget>()
    : GbsFlightWidgetActionBase<TWidget>("gbs-cancel-mode")
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

        var item = new MenuItem(ActionId, "Cancel")
        {
            Icon = ActionIcon,
            Description = "Cancel GBS mode transition",
            Order = 40,
        };

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
