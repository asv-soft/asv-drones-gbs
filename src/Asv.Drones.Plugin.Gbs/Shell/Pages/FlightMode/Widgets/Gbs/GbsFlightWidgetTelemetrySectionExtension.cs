using Asv.Avalonia;
using Asv.Drones.Api;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsFlightWidgetTelemetrySectionExtension(IServiceProvider services)
    : IExtensionFor<IGbsFlightWidget>
{
    public void Extend(IGbsFlightWidget context, CompositeDisposable contextDispose)
    {
        var device = context.Device ?? throw new NullReferenceException();
        string[] defaultItemIds =
        [
            BaseStationModeGbsTelemetryViewModel.RttId,
            AccuracyGbsTelemetryViewModel.RttId,
            ObservationGbsTelemetryViewModel.RttId,
            DGpsRateGbsTelemetryViewModel.RttId,
            LinkQualityGbsTelemetryViewModel.RttId,
            VisibleSatellitesGbsTelemetryViewModel.RttId,
        ];

        var vm = services.CreateViewModel<ITelemetrySection, TelemetrySectionArgs>(
            new TelemetrySectionArgs(device, defaultItemIds)
        );

        context.Sections.Add(vm);
    }
}
