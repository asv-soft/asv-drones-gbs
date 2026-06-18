using Asv.Avalonia;
using Asv.Cfg;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsAutoModeAction<TWidget>(
    IUnitService unitService,
    IConfiguration configuration
) : GbsFlightWidgetActionBase<TWidget>("gbs-auto-mode")
    where TWidget : class, IGbsFlightWidget<GbsClientDevice>
{
    public const MaterialIconKind ActionIcon = MaterialIconKind.Automatic;

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

        var item = new MenuItem(ActionId, "Enable Auto")
        {
            Icon = ActionIcon,
            Description = "Start GBS automatic RTK mode",
            Order = 10,
        };

        var canExecute = CreateModeCanExecute(
            gbs,
            mode => mode is AsvGbsCustomMode.AsvGbsCustomModeIdle,
            contextDispose
        );

        item.Command = canExecute
            .ToReactiveCommand<Unit>(
                (_, ct) => ExecuteWithErrorHandling(item, cancel => Execute(gbs, cancel), ct)
            )
            .DisposeItWith(contextDispose);

        return item;
    }

    private async ValueTask Execute(IAsvGbsExClient gbs, CancellationToken cancel)
    {
        using var viewModel = new AutoModeDialogViewModel(unitService, configuration);
        var dialog = new ContentDialog(viewModel)
        {
            Title = "Auto Mode",
            PrimaryButtonText = "Ok",
            IsSecondaryButtonEnabled = true,
            CloseButtonText = "Cancel",
        };

        viewModel.ApplyDialog(dialog);

        var result = await dialog.ShowAsync();
        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        var cfg = viewModel.GetResult();
        configuration.Set(cfg);

        await gbs.StartAutoMode((float)cfg.Observation, (float)cfg.Accuracy, cancel);
    }
}
