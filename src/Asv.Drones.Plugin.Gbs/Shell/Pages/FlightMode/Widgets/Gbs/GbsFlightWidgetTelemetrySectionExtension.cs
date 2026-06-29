using Asv.Avalonia;
using Asv.Drones.Api;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsFlightWidgetTelemetrySectionExtension(IServiceProvider services)
    : IExtensionFor<IGbsFlightWidget>
{
    public const string StaticId = "ext.flight-widget.telemetry.gbs";

    public string Id => StaticId;

    public void Extend(IGbsFlightWidget context, CompositeDisposable contextDispose)
    {
        var device = context.Device ?? throw new NullReferenceException();

        var vm = services.CreateViewModel<ITelemetrySection, TelemetrySectionArgs>(
            new TelemetrySectionArgs(device)
        );

        context.Sections.Add(vm);
    }
}
