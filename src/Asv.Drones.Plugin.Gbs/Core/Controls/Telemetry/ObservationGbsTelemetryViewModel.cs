using Asv.Avalonia;
using Asv.Common;
using Asv.Mavlink;
using Material.Icons;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class ObservationGbsTelemetryViewModel : GbsTelemetryViewModelBase
{
    public const string RttId = "gbs-observation";

    public ObservationGbsTelemetryViewModel()
    {
        ValueString = "30%";
    }

    public ObservationGbsTelemetryViewModel(
        IAsvGbsExClient gbsClient,
        IUnitService unitService,
        TimeSpan? networkErrorTimeout = null
    )
        : base(RttId, gbsClient, networkErrorTimeout)
    {
        Order = 1;
        Header = "Observation";
        Icon = MaterialIconKind.ClockOutline;

        var timeUnit =
            unitService.Units[TimeSpanUnit.Id] as TimeSpanUnit
            ?? throw new InvalidOperationException();

        GbsClient
            .ObservationSec.ObserveOnUIThreadDispatcher()
            .Subscribe(o => ValueString = timeUnit.PrintFromSiWithUnitsInRelativeTime(o))
            .DisposeItWith(Disposable);
    }
}
