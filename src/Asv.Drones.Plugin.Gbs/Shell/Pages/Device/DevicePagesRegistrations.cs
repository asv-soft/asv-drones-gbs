using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class DevicePagesRegistrations
{
    public static PagesRegistrations.Builder RegisterDevicePages(
        this PagesRegistrations.Builder builder
    )
    {
        builder.RegisterGbsPage();

        return builder;
    }
}
