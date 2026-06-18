using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsLinkQualityTelemetryFactory(IUnitService unitService) : ITelemetryItemFactory
{
    public string ItemId => LinkQualityGbsTelemetryViewModel.RttId;
    public string DisplayName => "GBS Link Quality";

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null
        && device.GetMicroservice<IHeartbeatClient>() is not null;

    public ITelemetryItem Create(in IClientDevice device) =>
        new LinkQualityGbsTelemetryViewModel(
            device.GetRequiredMicroservice<IHeartbeatClient>(),
            device.GetRequiredMicroservice<IAsvGbsExClient>(),
            unitService
        );

    public ITelemetryItem CreatePreview() => new LinkQualityGbsTelemetryViewModel();
}
