using Asv.Avalonia;
using Asv.IO;
using Asv.Mavlink;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsFlightWidgetSatelliteCountSectionExtension(IServiceProvider services)
    : IExtensionFor<IGbsFlightWidget>
{
    public const string StaticId = "ext.flight-widget.satellite-count.gbs";

    public string Id => StaticId;

    public void Extend(IGbsFlightWidget context, CompositeDisposable contextDispose)
    {
        var gbs = context.Device.GetMicroservice<IAsvGbsExClient>();
        if (gbs is null)
        {
            return;
        }

        var vm = services.CreateViewModel<IGbsSatelliteCountSection, GbsSatelliteCountSectionArgs>(
            new GbsSatelliteCountSectionArgs(gbs)
        );

        context.Sections.Add(vm);
    }
}
