using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsVisibleSatellitesTelemetryFactory : ITelemetryItemFactory
{
    public const string Id = "gbs-visible-satellites";

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITileViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var satellites = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .AllSatellites.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Select(value => value)
            .Prepend((byte)0)
            .ObserveOnUIThreadDispatcher();

        return new TelemetryViewModel<byte>(
            Id,
            satellites,
            static (tile, changes) => tile.Text = changes.ToString()
        )
        {
            Density = TileDensity.Inline,
            Header = RS.GbsVisibleSatellitesTelemetry_Header,
            ShortHeader = RS.GbsVisibleSatellitesTelemetry_ShortHeader,
            Units = RS.GbsVisibleSatellitesTelemetry_Units,
            Icon = MaterialIconKind.SatelliteVariant,
        };
    }
}
