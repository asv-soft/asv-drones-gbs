using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class AddCoordsRecordDialogViewModel : DialogViewModelBase
{
    public const string DialogId = GeoPointDialogViewModel.DialogId + ".add_record";

    private readonly SerialDisposable _sub;

    public AddCoordsRecordDialogViewModel()
        : this(null, NullMapService.Instance, DesignTime.UnitService, DesignTime.LoggerFactory)
    {
        DesignTime.ThrowIfNotDesignMode();
    }

    public AddCoordsRecordDialogViewModel(
        FixedModeConfig? baseValue,
        IMapService mapService,
        IUnitService unitService,
        ILoggerFactory loggerFactory
    )
        : base(DialogId, loggerFactory)
    {
        var distanceUnit = unitService.Units[DistanceUnit.Id];
        UnitSymbol = distanceUnit
            .CurrentUnitItem.Select(item => item.Symbol)
            .ObserveOnUIThreadDispatcher()
            .ToReadOnlyBindableReactiveProperty<string>()
            .DisposeItWith(Disposable);

        _sub = new SerialDisposable().DisposeItWith(Disposable);
        Name = new BindableReactiveProperty<string>(string.Empty).DisposeItWith(Disposable);
        Name.EnableValidationRoutable(
                name =>
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        return ValidationResult.FailAsNullOrWhiteSpace;
                    }

                    return ValidationResult.Success;
                },
                this,
                true
            )
            .DisposeItWith(Disposable);
        var accuracy = new ReactiveProperty<double>(0.01).DisposeItWith(Disposable);
        Accuracy = new BindableUnitProperty(
            nameof(Accuracy),
            accuracy,
            distanceUnit,
            loggerFactory
        ).DisposeItWith(Disposable);
        Accuracy.ForceValidate();
        GeoPointDialogViewModel = new GeoPointDialogViewModel(
            loggerFactory,
            unitService,
            mapService
        ).DisposeItWith(Disposable);

        if (baseValue is not null)
        {
            var startValue = new GeoPoint(
                baseValue.Latitude,
                baseValue.Longitude,
                baseValue.Altitude
            );
            Name.Value = baseValue.Name;
            Accuracy.ModelValue.Value = baseValue.Accuracy;
            GeoPointDialogViewModel.GeoPointProperty.ModelValue.Value = startValue;
        }
    }

    public BindableReactiveProperty<string> Name { get; }
    public BindableUnitProperty Accuracy { get; }
    public IReadOnlyBindableReactiveProperty<string> UnitSymbol { get; }
    public GeoPointDialogViewModel GeoPointDialogViewModel { get; }

    public override void ApplyDialog(ContentDialog dialog)
    {
        base.ApplyDialog(dialog);
        _sub.Disposable = IsValid
            .ThrottleLast(TimeSpan.FromMilliseconds(100))
            .CombineLatest(
                Accuracy.ViewValue,
                GeoPointDialogViewModel.IsValid,
                (isValid, _, isValidGeoPoint) => isValid && isValidGeoPoint && IsAccuracyValid()
            )
            .ObserveOnUIThreadDispatcher()
            .Subscribe(isValid => dialog.IsPrimaryButtonEnabled = isValid);
    }

    public override IEnumerable<IRoutable> GetChildren()
    {
        yield return Accuracy;
        yield return GeoPointDialogViewModel;
    }

    private bool IsAccuracyValid()
    {
        return !Accuracy.ViewValue.HasErrors;
    }
}
