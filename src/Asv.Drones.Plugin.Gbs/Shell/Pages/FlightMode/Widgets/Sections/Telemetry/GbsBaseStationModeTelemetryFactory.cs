using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsBaseStationModeTelemetryFactory : ITelemetryItemFactory
{
    public string ItemId => BaseStationModeGbsTelemetryViewModel.RttId;
    public string DisplayName => "GBS Mode";

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITelemetryItem Create(in IClientDevice device) =>
        new BaseStationModeGbsTelemetryViewModel(device.GetRequiredMicroservice<IAsvGbsExClient>());

    public ITelemetryItem CreatePreview() => new BaseStationModeGbsTelemetryViewModel();
}
