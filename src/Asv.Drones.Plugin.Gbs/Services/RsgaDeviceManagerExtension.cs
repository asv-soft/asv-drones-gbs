using System.Composition;
using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.IO;
using Asv.Mavlink;
using Avalonia.Media;
using Material.Icons;

namespace Asv.Drones.Plugin.Gbs;

[Export(typeof(IDeviceManagerExtension))]
[Shared]
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
            icon = GbsModule.DefaultIcon;
            return true;
        }

        icon = null;
        return false;
    }

    public bool TryGetDeviceBrush(DeviceId id, out AsvColorKind brush)
    {
        throw new NotImplementedException();
    }

    public bool TryGetDeviceBrush(DeviceId id, out IBrush? brush)
    {
        brush = null;
        return false;
    }
    
    public void Run(IDeviceManager deviceManager)
    {
        // do nothing
    }
}