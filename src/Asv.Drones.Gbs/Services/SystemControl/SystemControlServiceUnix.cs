using System.Diagnostics;

namespace Asv.Drones.Gbs;

public class SystemControlServiceUnix : SystemControlServiceBase
{
    protected override Task InternalReboot()
    {
        Process.Start("/usr/bin/sudo", "/bin/systemctl reboot");
        return Task.CompletedTask;
    }

    protected override Task InternalShutdown()
    {
        Process.Start("/usr/bin/sudo", "/bin/systemctl poweroff");
        return Task.CompletedTask;
    }
}
