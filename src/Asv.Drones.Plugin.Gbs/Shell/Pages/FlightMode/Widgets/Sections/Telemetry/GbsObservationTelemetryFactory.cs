using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsObservationTelemetryFactory(
    IUnitService unitService,
    ILoggerFactory loggerFactory
) : ITelemetryItemFactory
{
    public const string Id = "gbs-observation";
    private const AsvColorKind DefaultStatusColor = AsvColorKind.Info5;

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public IRttBoxViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var observation = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .ObservationSec.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Select(value => (double)value)
            .Prepend(0);

        return InternalCreate(observation);
    }

    public IRttBoxViewModel CreatePreview()
    {
        var observation = Observable.Return(30d).Concat(Observable.Never<double>());

        return InternalCreate(observation);
    }

    private IRttBoxViewModel InternalCreate(Observable<double> observation)
    {
        var timeUnit =
            unitService.Units[TimeSpanUnit.Id] as TimeSpanUnit
            ?? throw new InvalidOperationException();

        return new KeyValueRttBoxViewModel<double>(Id, loggerFactory, observation, null)
        {
            Header = "Observation",
            Icon = MaterialIconKind.ClockOutline,
            UpdateAction = (model, changes) =>
            {
                model[0, "Observation", null].ValueString =
                    timeUnit.PrintFromSiWithUnitsInRelativeTime(changes);
            },
            Status = DefaultStatusColor,
        };
    }
}
