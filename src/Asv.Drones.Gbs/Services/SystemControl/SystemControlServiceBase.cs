using Asv.Common;
using R3;

namespace Asv.Drones.Gbs;

public abstract class SystemControlServiceBase : AsyncDisposableWithCancel, ISystemControlService
{
    private readonly ReactiveProperty<bool> _isRebootRequested;

    protected SystemControlServiceBase()
    {
        _isRebootRequested = new ReactiveProperty<bool>(false);
        _isRebootRequested.RegisterTo(DisposeCancel);
    }

    public Task Do(SystemControlAction action)
    {
        return action switch
        {
            SystemControlAction.Reboot => InternalReboot(),
            SystemControlAction.Shutdown => InternalShutdown(),
            SystemControlAction.Restart => InternalRestart(),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };
    }

    protected abstract Task InternalReboot();

    protected virtual Task InternalRestart()
    {
        Environment.Exit(0);
        return Task.CompletedTask;
    }

    protected abstract Task InternalShutdown();
    public ReadOnlyReactiveProperty<bool> IsRebootRequested => _isRebootRequested;
}
