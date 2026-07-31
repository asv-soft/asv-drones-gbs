using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Cfg;
using Asv.Common;
using Asv.Modeling;
using Material.Icons;
using ObservableCollections;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class FixedModeDialogViewModel : GbsDialogViewModelBase
{
    public const string DialogId = "fixed-mode";

    private readonly PropertyComboBoxViewModel _savedCoordinatesSelection;
    private readonly PropertyGeoPointViewModel _geoPoint;
    private readonly PropertyUnitViewModel _accuracy;
    private readonly PropertyButtonViewModel _saveCurrentValues;
    private readonly ReactiveProperty<IHeadlinedViewModel?> _savedCoordinatesSelectionValue;
    private readonly ReactiveProperty<GeoPoint> _geoPointValue;
    private readonly ReactiveProperty<double> _accuracyValue;
    private readonly ObservableList<FixedModeConfig> _savedCoordinates;
    private readonly SerialDisposable _sub;
    private const double MinimumAccuracyDistance = 0.01;

    public FixedModeDialogViewModel()
        : this(DesignTime.UnitService, DesignTime.Configuration, DesignTime.DialogService)
    {
        DesignTime.ThrowIfNotDesignMode();
    }

    public FixedModeDialogViewModel(
        IUnitService unitService,
        IConfiguration configuration,
        IDialogService dialogService
    )
        : base(DialogId)
    {
        ArgumentNullException.ThrowIfNull(unitService);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dialogService);

        _sub = new SerialDisposable().DisposeItWith(Disposable);

        FieldsEditor = new PropertyEditorViewModel("fields")
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);

        var distanceUnit = unitService.Units[DistanceUnit.Id];

        _savedCoordinates = new ObservableList<FixedModeConfig>(
            configuration.Get<FixedModeSavedCoords>().Coords
        );

        _savedCoordinatesSelectionValue =
            new ReactiveProperty<IHeadlinedViewModel?>().DisposeItWith(Disposable);
        _savedCoordinatesSelection = new PropertyComboBoxReactive(
            "saved-coordinates",
            _savedCoordinatesSelectionValue
        )
        {
            Header = RS.FixedModeDialogView_SavedCoordinates_Header,
            ShortHeader = RS.FixedModeDialogView_SavedCoordinates_ShortHeader,
            Icon = MaterialIconKind.MapMarkerRadius,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);

        foreach (var config in _savedCoordinates)
        {
            _savedCoordinatesSelection.ItemsSource.Add(new SavedCoordsItem(config));
        }

        _savedCoordinatesSelectionValue
            .ObserveOnUIThreadDispatcher()
            .Subscribe(item =>
            {
                if (item is SavedCoordsItem savedCoords)
                {
                    SetValues(savedCoords.Config);
                }
            })
            .DisposeItWith(Disposable);

        _geoPointValue = new ReactiveProperty<GeoPoint>(GeoPoint.Zero).DisposeItWith(Disposable);
        _geoPoint = new PropertyGeoPointReactive(
            "geo-point",
            _geoPointValue,
            unitService,
            dialogService
        )
        {
            Header = RS.FixedModeDialogView_GeoPoint_Header,
            ShortHeader = RS.FixedModeDialogView_GeoPoint_ShortHeader,
            Icon = MaterialIconKind.CrosshairsGps,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _geoPoint.Latitude.EnableUnitValidationRoutable(this).DisposeItWith(Disposable);
        _geoPoint.Longitude.EnableUnitValidationRoutable(this).DisposeItWith(Disposable);
        _geoPoint.Altitude.EnableUnitValidationRoutable(this).DisposeItWith(Disposable);

        _accuracyValue = new ReactiveProperty<double>(MinimumAccuracyDistance).DisposeItWith(
            Disposable
        );
        _accuracy = new PropertyUnitReactive("accuracy", distanceUnit, _accuracyValue)
        {
            Header = RS.FixedModeDialogView_Accuracy_Header,
            ShortHeader = RS.FixedModeDialogView_Accuracy_ShortHeader,
            Icon = MaterialIconKind.CompareHorizontal,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _accuracy
            .EnableMinUnitValidationRoutable(this, MinimumAccuracyDistance)
            .DisposeItWith(Disposable);

        _saveCurrentValues = new PropertyButtonViewModel(
            "save-current-values",
            AddNewSavedCoordsAsync,
            IsValid
        )
        {
            Header = RS.FixedModeDialogView_SaveCurrentValues_Header,
            ShortHeader = RS.FixedModeDialogView_SaveCurrentValues_ShortHeader,
            Icon = MaterialIconKind.Add,
            IconColor = AsvColorKind.Success,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);

        FieldsEditor.ItemsSource.Add(_savedCoordinatesSelection);
        FieldsEditor.ItemsSource.Add(_geoPoint);
        FieldsEditor.ItemsSource.Add(_accuracy);
        FieldsEditor.ItemsSource.Add(_saveCurrentValues);
    }

    private void SetValues(FixedModeConfig cfg)
    {
        _geoPointValue.Value = new GeoPoint(cfg.Latitude, cfg.Longitude, cfg.Altitude);
        _accuracyValue.Value = cfg.Accuracy;
    }

    public override void ApplyDialog(ContentDialog dialog)
    {
        base.ApplyDialog(dialog);

        _sub.Disposable = IsValid
            .ObserveOnUIThreadDispatcher()
            .Subscribe(x => dialog.IsPrimaryButtonEnabled = x)
            .DisposeItWith(Disposable);
    }

    private async ValueTask AddNewSavedCoordsAsync(CancellationToken cancel = default)
    {
        if (!IsValid.Value)
        {
            return;
        }

        using var vm = new SetCoordsNameDialogViewModel();
        var dialog = new ContentDialog(vm)
        {
            Title = RS.FixedModeDialogView_NameDialog_Title,
            PrimaryButtonText = RS.Common_Ok,
            IsSecondaryButtonEnabled = true,
            CloseButtonText = Asv.Avalonia.RS.DialogButton_Cancel,
        };

        var result = await dialog.ShowAsync();

        if (result is not ContentDialogResult.Primary)
        {
            return;
        }

        var geoPoint = _geoPointValue.CurrentValue;
        var acc = _accuracyValue.CurrentValue;
        var name = vm.Name;
        var config = new FixedModeConfig
        {
            Name = name,
            Latitude = geoPoint.Latitude,
            Longitude = geoPoint.Longitude,
            Altitude = geoPoint.Altitude,
            Accuracy = acc,
        };

        _savedCoordinates.Add(config);
        var item = new SavedCoordsItem(config);
        _savedCoordinatesSelection.ItemsSource.Add(item);
        _savedCoordinatesSelectionValue.Value = item;
    }

    public FixedModeConfig GetResult()
    {
        var geoPoint = _geoPointValue.CurrentValue;
        return new FixedModeConfig
        {
            Name =
                (_savedCoordinatesSelectionValue.CurrentValue as SavedCoordsItem)?.Config.Name
                ?? "tmp",
            Latitude = geoPoint.Latitude,
            Longitude = geoPoint.Longitude,
            Altitude = geoPoint.Altitude,
            Accuracy = _accuracyValue.CurrentValue,
        };
    }

    public FixedModeSavedCoords GetSavedCoordinates()
    {
        return new FixedModeSavedCoords { Coords = _savedCoordinates.ToList() };
    }

    public PropertyEditorViewModel FieldsEditor { get; }

    public override IEnumerable<IViewModel> GetChildren()
    {
        yield return FieldsEditor;
    }

    private sealed class SavedCoordsItem : HeadlinedViewModel
    {
        public SavedCoordsItem(FixedModeConfig config)
            : base(NavId.GenerateRandomAsString())
        {
            Config = config;
            Header = config.Name;
            Description =
                $"Lat: {config.Latitude}, Lon: {config.Longitude}, Alt: {config.Altitude}, Acc: {config.Accuracy}";
            Icon = MaterialIconKind.CrosshairsGps;
            IconColor = AsvColorKind.Info5;
        }

        public FixedModeConfig Config { get; }
    }
}
