using Asv.Avalonia;
using Asv.Cfg;
using Asv.Common;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class AutoModeConfig
{
    public double Observation { get; set; } = AutoModeDialogViewModel.MinimumObservationTime;
    public double Accuracy { get; set; } = AutoModeDialogViewModel.MinimumAccuracyDistance;
}

public class AutoModeDialogViewModel : GbsDialogViewModelBase
{
    public const string DialogId = "auto-mode";

    public const double MinimumAccuracyDistance = 0.1;
    public const double MinimumObservationTime = 1;

    private readonly PropertyUnitViewModel _accuracy;
    private readonly PropertyUnitViewModel _observation;
    private readonly SerialDisposable _sub;
    private readonly ReactiveProperty<double> _observationValue;
    private readonly ReactiveProperty<double> _accuracyValue;

    public AutoModeDialogViewModel()
        : this(DesignTime.UnitService, DesignTime.Configuration)
    {
        DesignTime.ThrowIfNotDesignMode();
    }

    public AutoModeDialogViewModel(IUnitService unitService, IConfiguration configuration)
        : base(DialogId)
    {
        ArgumentNullException.ThrowIfNull(unitService);
        ArgumentNullException.ThrowIfNull(configuration);

        _sub = new SerialDisposable().DisposeItWith(Disposable);

        FieldsEditor = new PropertyEditorViewModel("fields")
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);

        var distanceUnit = unitService.Units[DistanceUnit.Id];
        var timeUnit = unitService.Units[TimeSpanUnit.Id];
        var autoModeConfig = configuration.Get<AutoModeConfig>();

        _accuracyValue = new ReactiveProperty<double>(
            Math.Max(autoModeConfig.Accuracy, MinimumAccuracyDistance)
        ).DisposeItWith(Disposable);
        _accuracy = new PropertyUnitReactive("accuracy", distanceUnit, _accuracyValue)
        {
            Header = RS.AutoModeDialogView_Accuracy_Header,
            ShortHeader = RS.AutoModeDialogView_Accuracy_ShortHeader,
            Icon = MaterialIconKind.CompareHorizontal,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _accuracy
            .EnableMinUnitValidationRoutable(this, MinimumAccuracyDistance)
            .DisposeItWith(Disposable);

        _observationValue = new ReactiveProperty<double>(
            Math.Max(autoModeConfig.Observation, MinimumObservationTime)
        ).DisposeItWith(Disposable);
        _observation = new PropertyUnitReactive("observation", timeUnit, _observationValue)
        {
            Header = RS.AutoModeDialogView_Observation_Header,
            ShortHeader = RS.AutoModeDialogView_Observation_ShortHeader,
            Icon = MaterialIconKind.Clockwise,
            IconColor = AsvColorKind.Info5,
        }
            .SetRoutableParent(this)
            .DisposeItWith(Disposable);
        _observation
            .EnableMinUnitValidationRoutable(this, MinimumObservationTime)
            .DisposeItWith(Disposable);

        FieldsEditor.ItemsSource.Add(_accuracy);
        FieldsEditor.ItemsSource.Add(_observation);
    }

    public override void ApplyDialog(ContentDialog dialog)
    {
        base.ApplyDialog(dialog);

        _sub.Disposable = IsValid
            .ObserveOnUIThreadDispatcher()
            .Subscribe(isValid => dialog.IsPrimaryButtonEnabled = isValid);
    }

    public AutoModeConfig GetResult()
    {
        return new AutoModeConfig
        {
            Accuracy = _accuracyValue.Value,
            Observation = _observationValue.Value,
        };
    }

    public PropertyEditorViewModel FieldsEditor { get; }

    public override IEnumerable<IViewModel> GetChildren()
    {
        yield return FieldsEditor;
    }
}
