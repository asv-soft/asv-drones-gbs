using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsObservationTelemetryFactory(IUnitService unitService) : ITelemetryItemFactory
{
    public string ItemId => ObservationGbsTelemetryViewModel.RttId;
    public string DisplayName => "GBS Observation";

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITelemetryItem Create(in IClientDevice device) =>
        new ObservationGbsTelemetryViewModel(
            device.GetRequiredMicroservice<IAsvGbsExClient>(),
            unitService
        );

    public ITelemetryItem CreatePreview() => new ObservationGbsTelemetryViewModel();
}
