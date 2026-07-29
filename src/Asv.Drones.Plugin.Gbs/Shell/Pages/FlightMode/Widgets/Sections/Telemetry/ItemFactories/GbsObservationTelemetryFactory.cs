using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsObservationTelemetryFactory(IUnitService unitService) : ITelemetryItemFactory
{
    public const string Id = "gbs-observation";

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITileViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var observation = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .ObservationSec.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Select(value => (double)value)
            .Prepend(0)
            .ObserveOnUIThreadDispatcher();

        var timeUnit = unitService.GetRequiredUnitOfType<TimeSpanUnit>(TimeSpanUnit.Id);

        return new TelemetryViewModel<double>(Id, observation, Update)
        {
            Density = TileDensity.Inline,
            Header = RS.GbsObservationTelemetry_Header,
            ShortHeader = RS.GbsObservationTelemetry_ShortHeader,
            Icon = MaterialIconKind.ClockOutline,
        };

        void Update(TelemetryViewModel<double> tile, double changes)
        {
            tile.Text = timeUnit.PrintFromSiInRelativeTime(changes);
            tile.Units = timeUnit.GetRelativeTimeUnitItem(changes).Symbol;
        }
    }
}
