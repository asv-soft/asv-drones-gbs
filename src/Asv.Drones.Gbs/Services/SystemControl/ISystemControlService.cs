using R3;

namespace Asv.Drones.Gbs;

public enum SystemControlAction
{
    /// <summary>
    /// Request system reboot
    /// ASV_SDR_SYSTEM_CONTROL_ACTION_REBOOT
    /// </summary>
    Reboot = 1,

    /// <summary>
    /// Request system shutdown
    /// ASV_SDR_SYSTEM_CONTROL_ACTION_SHUTDOWN
    /// </summary>
    Shutdown = 2,

    /// <summary>
    /// Request software reboot
    /// ASV_SDR_SYSTEM_CONTROL_ACTION_RESTART
    /// </summary>
    Restart = 3,
}

public interface ISystemControlService
{
    Task Do(SystemControlAction action);
    ReadOnlyReactiveProperty<bool> IsRebootRequested { get; }
}
