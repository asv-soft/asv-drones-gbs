using Asv.Avalonia;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Api;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsAutoModeAction<TTarget>(
    IUnitService unitService,
    IConfiguration configuration
) : GbsModeActionBase<TTarget>("auto-mode")
    where TTarget : class, IViewModel, IDeviceActionTarget<GbsClientDevice>
{
    public const MaterialIconKind ActionIcon = MaterialIconKind.Automatic;

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

        var item = CreateMenuItem(RS.GbsAutoModeAction_Header);
        item.Icon = ActionIcon;
        item.Description = RS.GbsAutoModeAction_Description;
        item.Order = 10;

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
            Title = RS.GbsAutoModeAction_Dialog_Title,
            PrimaryButtonText = RS.Common_Ok,
            IsSecondaryButtonEnabled = true,
            CloseButtonText = Asv.Avalonia.RS.DialogButton_Cancel,
        };

        viewModel.ApplyDialog(dialog);

        var result = await dialog.ShowAsync();
        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        var cfg = viewModel.GetResult();
        configuration.Set(cfg);

        await gbs.StartAutoMode((float)cfg.Accuracy, (float)cfg.Observation, cancel);
    }
}
