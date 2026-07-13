using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;

namespace Asv.Drones.Plugin.Gbs;

public class GbsDeviceManagerExtension : IDeviceManagerExtension
{
    public void Configure(IProtocolBuilder builder)
    {
        // do nothing
    }

    public void Configure(IDeviceExplorerBuilder builder)
    {
        // do nothing
    }

    public bool TryGetIcon(DeviceId id, out MaterialIconKind? icon)
    {
        if (id.DeviceClass == GbsClientDevice.DeviceClass)
        {
            icon = GbsPluginRegistrations.DefaultIcon;
            return true;
        }

        icon = null;
        return false;
    }

    public bool TryGetDeviceBrush(DeviceId id, out AsvColorKind brush)
    {
        brush = AsvColorKind.None;
        return false;
    }

    public void Run(IDeviceManager deviceManager)
    {
        // do nothing
    }
}
