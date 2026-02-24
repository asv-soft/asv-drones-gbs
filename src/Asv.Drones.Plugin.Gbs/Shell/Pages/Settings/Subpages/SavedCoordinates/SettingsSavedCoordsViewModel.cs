using System.Composition;
using System.Windows.Input;
using Asv.Avalonia;
using Asv.Cfg;
using Asv.Common;
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

[ExportSettings(SubPageId)]
public class SettingsSavedCoordsViewModel : SettingsSubPage
{
    public const string SubPageId = "saved_coords";
    public static MaterialIconKind Icon => GbsModule.DefaultIcon;

    private readonly YesOrNoDialogPrefab _yesOrNoDialog;
    private readonly INavigationService _navigationService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IUnitService _unitService;
    private readonly ObservableList<FixedModeConfig> _savedCoordinates;
    private readonly IConfiguration _cfg;

    public SettingsSavedCoordsViewModel()
        : this(
            DesignTime.DialogService,
            DesignTime.Configuration,
            DesignTime.Navigation,
            DesignTime.UnitService,
            DesignTime.LoggerFactory
        )
    {
        DesignTime.ThrowIfNotDesignMode();
    }

    [ImportingConstructor]
    public SettingsSavedCoordsViewModel(
        IDialogService dialogService,
        IConfiguration configuration,
        INavigationService navigationService,
        IUnitService unitService,
        ILoggerFactory loggerFactory
    )
        : base(SubPageId, loggerFactory)
    {
        _yesOrNoDialog = dialogService.GetDialogPrefab<YesOrNoDialogPrefab>();
        _navigationService = navigationService;
        _loggerFactory = loggerFactory;
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
            new MenuItem("add", RS.SettingsSavedCoordsViewModel_MenuItem_Add_Header, loggerFactory)
            {
                Order = 1,
                Icon = MaterialIconKind.Add,
                Command = AddNewItemCommand,
            }
        );

        Menu.Add(
            new MenuItem(
                "remove",
                RS.SettingsSavedCoordsViewModel_MenuItem_Remove_Header,
                loggerFactory
            )
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
            _loggerFactory
        );
        var dialog = new ContentDialog(vm, _navigationService)
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

        var config = new FixedModeConfig
        {
            Accuracy = vm.Accuracy.ModelValue.Value,
            Name = vm.Name.Value,
            Latitude = vm.GeoPointDialogViewModel.GeoPointProperty.ModelValue.Value.Latitude,
            Longitude = vm.GeoPointDialogViewModel.GeoPointProperty.ModelValue.Value.Longitude,
            Altitude = vm.GeoPointDialogViewModel.GeoPointProperty.ModelValue.Value.Altitude,
        };
        _savedCoordinates.Add(config);

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

    public override IExportInfo Source => GbsModule.Instance;
}
