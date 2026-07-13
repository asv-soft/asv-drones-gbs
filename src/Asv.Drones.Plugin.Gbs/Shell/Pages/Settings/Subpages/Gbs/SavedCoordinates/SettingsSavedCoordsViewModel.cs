using System.Windows.Input;
using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Cfg;
using Asv.Common;
using Asv.Modeling;
using Material.Icons;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class FixedModeConfig
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double Altitude { get; set; }
    public double Accuracy { get; set; } = 0.01;
    public required string Name { get; set; }
}

public class FixedModeSavedCoords
{
    public IList<FixedModeConfig> Coords { get; set; } = [];
}

public class SettingsSavedCoordsViewModel : SettingsSubPage
{
    public const string SubPageId = "saved_coords";
    public static MaterialIconKind Icon => GbsPluginRegistrations.DefaultIcon;

    private readonly IDialogService _dialogService;
    private readonly YesOrNoDialogPrefab _yesOrNoDialog;
    private readonly IUnitService _unitService;
    private readonly ObservableList<FixedModeConfig> _savedCoordinates;
    private readonly IConfiguration _cfg;

    public SettingsSavedCoordsViewModel()
        : this(
            DesignTimeSettingsSubPageContext.Instance,
            DesignTime.DialogService,
            DesignTime.Configuration,
            NullMapService.Instance,
            DesignTime.UnitService,
            DesignTime.LoggerFactory
        )
    {
        DesignTime.ThrowIfNotDesignMode();
    }

    public SettingsSavedCoordsViewModel(
        ITreeSubPageContext<ISettingsPage> context,
        IDialogService dialogService,
        IConfiguration configuration,
        IMapService mapService,
        IUnitService unitService,
        ILoggerFactory loggerFactory
    )
        : base(SubPageId, context)
    {
        _dialogService = dialogService;
        _yesOrNoDialog = dialogService.GetDialogPrefab<YesOrNoDialogPrefab>();
        _unitService = unitService;
        _cfg = configuration;

        _savedCoordinates = new ObservableList<FixedModeConfig>(
            configuration.Get<FixedModeSavedCoords>().Coords
        );
        SavedCoordinates = _savedCoordinates.ToNotifyCollectionChanged().DisposeItWith(Disposable);

        SelectedCoordsItem = new BindableReactiveProperty<FixedModeConfig?>(null).DisposeItWith(
            Disposable
        );
        var canRemove = Observable
            .Merge(SelectedCoordsItem.Select(x => x is not null))
            .ObserveOnUIThreadDispatcher()
            .Where(_ =>
            {
                if (SelectedCoordsItem.Value is null)
                {
                    return false;
                }

                return _savedCoordinates.Contains(SelectedCoordsItem.Value);
            })
            .ToReadOnlyReactiveProperty()
            .DisposeItWith(Disposable);

        AddNewItemCommand = new ReactiveCommand(AddNewItem).DisposeItWith(Disposable);
        RemoveItemCommand = canRemove.ToReactiveCommand<Unit>(RemoveItem).DisposeItWith(Disposable);

        Menu.Add(
            new MenuItem("add", RS.SettingsSavedCoordsViewModel_MenuItem_Add_Header)
            {
                Order = 1,
                Icon = MaterialIconKind.Add,
                Command = AddNewItemCommand,
            }
        );

        Menu.Add(
            new MenuItem("remove", RS.SettingsSavedCoordsViewModel_MenuItem_Remove_Header)
            {
                Order = 2,
                Icon = MaterialIconKind.Delete,
                IconColor = AsvColorKind.Error,
                Command = RemoveItemCommand,
            }
        );
    }

    private async ValueTask AddNewItem(Unit unit, CancellationToken cancel = default)
    {
        cancel.ThrowIfCancellationRequested();
        using var vm = new AddCoordsRecordDialogViewModel(
            SelectedCoordsItem.Value,
            _unitService,
            _dialogService
        );
        var dialog = new ContentDialog(vm)
        {
            Title = RS.SettingsSavedCoordsViewModel_AddNewItemDialog_Title,
            PrimaryButtonText = RS.SettingsSavedCoordsViewModel_AddNewItemDialog_PrimaryButtonText,
            IsSecondaryButtonEnabled = true,
            CloseButtonText = RS.SettingsSavedCoordsViewModel_AddNewItemDialog_CloseButtonText,
        };
        vm.ApplyDialog(dialog);
        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        _savedCoordinates.Add(vm.GetResult());

        UpdateCfg(_cfg);
    }

    private async ValueTask RemoveItem(Unit unit, CancellationToken cancel = default)
    {
        var payload = new YesOrNoDialogPayload
        {
            Title = RS.SettingsSavedCoordsViewModel_RemoveItemDialog_Title,
            Message = RS.SettingsSavedCoordsViewModel_RemoveItemDialog_Message,
        };

        var result = await _yesOrNoDialog.ShowDialogAsync(payload);

        if (!result)
        {
            return;
        }

        if (SelectedCoordsItem.Value is not null)
        {
            _savedCoordinates.Remove(SelectedCoordsItem.Value);
            SelectedCoordsItem.Value = null;
            UpdateCfg(_cfg);
        }
    }

    private void UpdateCfg(IConfiguration configuration)
    {
        var savedCoordsConfig = new FixedModeSavedCoords { Coords = _savedCoordinates.ToList() };

        configuration.Set(savedCoordsConfig);
    }

    public ICommand AddNewItemCommand { get; }
    public ICommand? RemoveItemCommand { get; }

    public BindableReactiveProperty<FixedModeConfig?> SelectedCoordsItem { get; }
    public INotifyCollectionChangedSynchronizedViewList<FixedModeConfig> SavedCoordinates { get; }
}

internal sealed class DesignTimeSettingsSubPageContext : ITreeSubPageContext<ISettingsPage>
{
    public static ITreeSubPageContext<ISettingsPage> Instance { get; } =
        new DesignTimeSettingsSubPageContext();

    public NavArgs Args => default;

    public ISettingsPage Context => null!;
}
