using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class DevicePagesMixin
{
    public static PagesMixin.Builder RegisterDevicePages(this PagesMixin.Builder builder)
    {
        builder.RegisterGbsPage();

        return builder;
    }
}
