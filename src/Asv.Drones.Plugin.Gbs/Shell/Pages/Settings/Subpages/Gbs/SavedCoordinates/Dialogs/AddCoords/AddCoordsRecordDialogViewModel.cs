using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Common;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class AddCoordsRecordDialogViewModel : GbsDialogViewModelBase
{
    public const string DialogId = "add-record";

    private readonly SerialDisposable _sub;
    private readonly PropertyTextBoxViewModel _name;
    private readonly PropertyGeoPointViewModel _geoPoint;
    private readonly PropertyUnitViewModel _accuracy;
    private readonly ReactiveProperty<string?> _nameValue;
    private readonly ReactiveProperty<GeoPoint> _geoPointValue;
    private readonly ReactiveProperty<double> _accuracyValue;

    public AddCoordsRecordDialogViewModel()
        : this(null, DesignTime.UnitService, DesignTime.DialogService)
    {
        DesignTime.ThrowIfNotDesignMode();
    }

    public AddCoordsRecordDialogViewModel(
        FixedModeConfig? baseValue,
        IUnitService unitService,
        IDialogService dialogService
    )
        : base(DialogId)
    {
        ArgumentNullException.ThrowIfNull(unitService);
        ArgumentNullException.ThrowIfNull(dialogService);

        _sub = new SerialDisposable().DisposeItWith(Disposable);

        FieldsEditor = new PropertyEditorViewModel("fields")
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);

        _nameValue = new ReactiveProperty<string?>().DisposeItWith(Disposable);
        _name = new PropertyTextBoxReactive("name", _nameValue)
        {
            Header = RS.AddCoordsRecordDialogView_InputField_Name,
            ShortHeader = RS.AddCoordsRecordDialogView_InputField_Name_ShortHeader,
            Icon = MaterialIconKind.Rename,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _name
            .Text.EnableValidationRoutable(
                value =>
                    string.IsNullOrEmpty(value)
                        ? ValidationResult.FailAsNullOrWhiteSpace
                        : ValidationResult.Success,
                this,
                true
            )
            .DisposeItWith(Disposable);

        _geoPointValue = new ReactiveProperty<GeoPoint>(GeoPoint.Zero).DisposeItWith(Disposable);
        _geoPoint = new PropertyGeoPointReactive(
            "geo-point",
            _geoPointValue,
            unitService,
            dialogService
        )
        {
            Header = RS.AddCoordsRecordDialogView_InputField_GeoPoint,
            ShortHeader = RS.AddCoordsRecordDialogView_InputField_GeoPoint_ShortHeader,
            Icon = MaterialIconKind.CrosshairsGps,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _geoPoint.Latitude.EnableUnitValidationRoutable(this).DisposeItWith(Disposable);
        _geoPoint.Longitude.EnableUnitValidationRoutable(this).DisposeItWith(Disposable);
        _geoPoint.Altitude.EnableUnitValidationRoutable(this).DisposeItWith(Disposable);

        _accuracyValue = new ReactiveProperty<double>(0.01).DisposeItWith(Disposable);
        _accuracy = new PropertyUnitReactive(
            "accuracy",
            unitService.Units[DistanceUnit.Id],
            _accuracyValue
        )
        {
            Header = RS.AddCoordsRecordDialogView_InputField_Accuracy,
            ShortHeader = RS.AddCoordsRecordDialogView_InputField_Accuracy_ShortHeader,
            Icon = MaterialIconKind.CompareHorizontal,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _accuracy.EnableUnitValidationRoutable(this).DisposeItWith(Disposable);

        FieldsEditor.ItemsSource.Add(_name);
        FieldsEditor.ItemsSource.Add(_geoPoint);
        FieldsEditor.ItemsSource.Add(_accuracy);

        if (baseValue is not null)
        {
            _nameValue.Value = baseValue.Name;
            _geoPointValue.Value = new GeoPoint(
                baseValue.Latitude,
                baseValue.Longitude,
                baseValue.Altitude
            );
            _accuracyValue.Value = baseValue.Accuracy;
        }
    }

    public PropertyEditorViewModel FieldsEditor { get; }

    public FixedModeConfig GetResult()
    {
        var geoPoint = _geoPointValue.CurrentValue;
        return new FixedModeConfig
        {
            Accuracy = _accuracyValue.CurrentValue,
            Name = _nameValue.CurrentValue ?? throw new Exception("Name should not be null"),
            Latitude = geoPoint.Latitude,
            Longitude = geoPoint.Longitude,
            Altitude = geoPoint.Altitude,
        };
    }

    public override void ApplyDialog(ContentDialog dialog)
    {
        base.ApplyDialog(dialog);
        _sub.Disposable = IsValid
            .ObserveOnUIThreadDispatcher()
            .Subscribe(isValid => dialog.IsPrimaryButtonEnabled = isValid);
    }

    public override IEnumerable<IViewModel> GetChildren()
    {
        yield return FieldsEditor;
    }
}
