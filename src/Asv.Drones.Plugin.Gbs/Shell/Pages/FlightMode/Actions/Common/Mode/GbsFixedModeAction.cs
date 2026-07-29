using Asv.Avalonia;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Api;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsFixedModeAction<TTarget>(
    IUnitService unitService,
    IConfiguration configuration,
    IDialogService dialogService
) : GbsModeActionBase<TTarget>("fixed-mode")
    where TTarget : class, IViewModel, IDeviceActionTarget<GbsClientDevice>
{
    public const MaterialIconKind ActionIcon = MaterialIconKind.CrosshairsGps;

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

        var item = CreateMenuItem(RS.GbsFixedModeAction_Header);
        item.Icon = ActionIcon;
        item.Description = RS.GbsFixedModeAction_Description;
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
            Title = RS.GbsFixedModeAction_Dialog_Title,
            PrimaryButtonText = RS.Common_Ok,
            IsSecondaryButtonEnabled = true,
            CloseButtonText = Asv.Avalonia.RS.DialogButton_Cancel,
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
