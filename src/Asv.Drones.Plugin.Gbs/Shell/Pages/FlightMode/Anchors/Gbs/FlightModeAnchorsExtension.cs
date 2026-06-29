using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Asv.Modeling;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class FlightModeAnchorsExtension(IDeviceManager conn, IExtensionService ext)
    : IExtensionFor<IFlightModePage>
{
    public const string StaticId = "ext.flight-mode.anchors.gbs";

    public string Id => StaticId;

    public void Extend(IFlightModePage context, CompositeDisposable contextDispose)
    {
        conn.Explorer.InitializedDevices.PopulateTo(
                context.Map.Anchors,
                TryCreateAnchor,
                RemoveAnchor
            )
            .DisposeItWith(contextDispose);
    }

    private GbsAnchor? TryCreateAnchor(IClientDevice device)
    {
        if (device is not GbsClientDevice gbs)
        {
            return null;
        }

        return new GbsAnchor(gbs, conn, ext);
    }

    private static bool RemoveAnchor(IClientDevice dev, GbsAnchor anchor)
    {
        return anchor.Device.Id == dev.Id;
    }
}
