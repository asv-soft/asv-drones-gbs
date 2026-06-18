using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsVisibleSatellitesTelemetryFactory : ITelemetryItemFactory
{
    public string ItemId => VisibleSatellitesGbsTelemetryViewModel.RttId;
    public string DisplayName => "GBS Satellites";

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITelemetryItem Create(in IClientDevice device) =>
        new VisibleSatellitesGbsTelemetryViewModel(
            device.GetRequiredMicroservice<IAsvGbsExClient>()
        );

    public ITelemetryItem CreatePreview() => new VisibleSatellitesGbsTelemetryViewModel();
}
