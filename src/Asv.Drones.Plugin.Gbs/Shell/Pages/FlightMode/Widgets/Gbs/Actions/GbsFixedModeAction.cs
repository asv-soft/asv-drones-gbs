using Asv.Avalonia;
using Asv.Cfg;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsFixedModeAction<TWidget>(
    IUnitService unitService,
    IConfiguration configuration,
    IDialogService dialogService
) : GbsFlightWidgetActionBase<TWidget>("gbs-fixed-mode")
    where TWidget : class, IGbsFlightWidget<GbsClientDevice>
{
    public const MaterialIconKind ActionIcon = MaterialIconKind.CrosshairsGps;

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

        var item = CreateMenuItem("Enable Fixed");
        item.Icon = ActionIcon;
        item.Description = "Start GBS fixed RTK mode";
        item.Order = 20;

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
        using var viewModel = new FixedModeDialogViewModel(
            unitService,
            configuration,
            dialogService
        );
        var dialog = new ContentDialog(viewModel)
        {
            Title = "Fixed Mode",
            PrimaryButtonText = "Ok",
            IsSecondaryButtonEnabled = true,
            CloseButtonText = "Cancel",
        };

        viewModel.ApplyDialog(dialog);

        var result = await dialog.ShowAsync();
        configuration.Set(viewModel.GetSavedCoordinates());
        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        var cfg = viewModel.GetResult();

        await gbs.StartFixedMode(
            new GeoPoint(cfg.Latitude, cfg.Longitude, cfg.Altitude),
            (float)cfg.Accuracy,
            cancel
        );
    }
}
