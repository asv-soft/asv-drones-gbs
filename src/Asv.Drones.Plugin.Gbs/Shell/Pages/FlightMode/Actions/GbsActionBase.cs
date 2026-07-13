using Asv.Avalonia;
using Asv.Avalonia.InfoMessage;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;

namespace Asv.Drones.Plugin.Gbs;

public abstract class GbsActionBase<TTarget>(string id)
    : DeviceMenuAction<TTarget>("gbs-action.", id)
    where TTarget : class, IViewModel, IDeviceActionTarget<GbsClientDevice>
{
    public override string Id { get; } = $"ext.flight-widget.action.gbs.{id}";

    protected static IAsvGbsExClient? TryGetGbsClient(TTarget target) =>
        target.Device.GetMicroservice<IAsvGbsExClient>();

    protected static async ValueTask ExecuteWithErrorHandling(
        IViewModel owner,
        Func<CancellationToken, ValueTask> execute,
        CancellationToken cancel
    )
    {
        try
        {
            await execute(cancel);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await owner.RiseShellErrorMessage("GBS action", ex.Message, ex, cancel);
        }
    }
}
