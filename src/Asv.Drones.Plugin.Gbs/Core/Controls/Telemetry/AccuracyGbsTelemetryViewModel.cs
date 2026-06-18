using Asv.Avalonia;
using Asv.Common;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class AccuracyGbsTelemetryViewModel : GbsTelemetryViewModelBase
{
    public const string RttId = "gbs-accuracy";

    public AccuracyGbsTelemetryViewModel()
    {
        ValueString = "5 m";
    }

    public AccuracyGbsTelemetryViewModel(
        IAsvGbsExClient gbsClient,
        IUnitService unitService,
        TimeSpan? networkErrorTimeout = null
    )
        : base(RttId, gbsClient, networkErrorTimeout)
    {
        Order = 1;
        Header = "Accuracy";
        Icon = MaterialIconKind.CrosshairsGps;
        var unit = unitService.Units[DistanceUnit.Id]; // TODO: make accuracy unit

        GbsClient
            .AccuracyMeter.ObserveOnUIThreadDispatcher()
            .Subscribe(v => ValueString = unit.CurrentUnitItem.Value.PrintFromSiWithUnits(v))
            .DisposeItWith(Disposable);
    }
}
