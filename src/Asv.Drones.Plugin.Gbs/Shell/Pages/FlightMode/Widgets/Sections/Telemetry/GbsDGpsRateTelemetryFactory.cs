using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsDGpsRateTelemetryFactory : ITelemetryItemFactory
{
    public string ItemId => DGpsRateGbsTelemetryViewModel.RttId;
    public string DisplayName => "GBS DGPS Rate";

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITelemetryItem Create(in IClientDevice device) =>
        new DGpsRateGbsTelemetryViewModel(device.GetRequiredMicroservice<IAsvGbsExClient>());

    public ITelemetryItem CreatePreview() => new DGpsRateGbsTelemetryViewModel();
}
