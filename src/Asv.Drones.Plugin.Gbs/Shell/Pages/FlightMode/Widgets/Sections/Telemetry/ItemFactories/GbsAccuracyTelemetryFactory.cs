using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsAccuracyTelemetryFactory(IUnitService unitService) : ITelemetryItemFactory
{
    public const string Id = "gbs-accuracy";

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITileViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var accuracy = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .AccuracyMeter.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Prepend(double.NaN)
            .CombineLatest(
                unitService.Units[DistanceUnit.Id].CurrentUnitItem,
                (value, unit) => new GbsAccuracyTelemetryData(value, unit)
            )
            .ObserveOnUIThreadDispatcher();

        return new TelemetryViewModel<GbsAccuracyTelemetryData>(Id, accuracy, Update)
        {
            Density = TileDensity.Inline,
            Header = RS.GbsAccuracyTelemetry_Header,
            ShortHeader = RS.GbsAccuracyTelemetry_ShortHeader,
            Icon = MaterialIconKind.CrosshairsGps,
        };

        static void Update(
            TelemetryViewModel<GbsAccuracyTelemetryData> tile,
            GbsAccuracyTelemetryData changes
        )
        {
            tile.Text = changes.Unit.PrintFromSi(changes.Value, "F2");
            tile.Units = changes.Unit.Symbol;
        }
    }
}

#pragma warning disable SA1313
public readonly record struct GbsAccuracyTelemetryData(double Value, IUnitItem Unit);
#pragma warning restore SA1313
