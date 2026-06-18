using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsAccuracyTelemetryFactory(IUnitService unitService) : ITelemetryItemFactory
{
    public string ItemId => AccuracyGbsTelemetryViewModel.RttId;
    public string DisplayName => "GBS Accuracy";

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITelemetryItem Create(in IClientDevice device) =>
        new AccuracyGbsTelemetryViewModel(
            device.GetRequiredMicroservice<IAsvGbsExClient>(),
            unitService
        );

    public ITelemetryItem CreatePreview() => new AccuracyGbsTelemetryViewModel();
}
