using System.Diagnostics;

namespace Asv.Drones.Gbs;

public class SystemControlServiceWindows : SystemControlServiceBase
{
    protected override Task InternalReboot()
    {
        Process.Start("shutdown", "/r /t 0");
        return Task.CompletedTask;
    }

    protected override Task InternalShutdown()
    {
        Process.Start("shutdown", "/s /t 0");
        return Task.CompletedTask;
    }
}
