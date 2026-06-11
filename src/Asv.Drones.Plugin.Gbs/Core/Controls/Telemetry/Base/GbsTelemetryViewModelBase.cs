using Asv.Avalonia;
using Asv.Mavlink;

namespace Asv.Drones.Plugin.Gbs;

public abstract class GbsTelemetryViewModelBase : SingleRttBoxViewModel, IGbsTelemetry
{
    public const string BaseId = "rtt-gbs";

    public GbsTelemetryViewModelBase()
        : base(DesignTime.Id.TypeId)
    {
        ItemId = DesignTime.Id.TypeId;
        GbsClient = null!;
    }

    protected GbsTelemetryViewModelBase(
        string id,
        IAsvGbsExClient gbsClient,
        TimeSpan? networkErrorTimeout = null
    )
        : base($"{BaseId}.{id}", networkErrorTimeout)
    {
        ItemId = id;
        GbsClient = gbsClient;
        Status = IGbsTelemetry.DefaultStatusColor;
    }

    public string ItemId { get; }

    protected IAsvGbsExClient GbsClient { get; }
}
