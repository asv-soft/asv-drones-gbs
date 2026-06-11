using Asv.Avalonia;
using Asv.Mavlink;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public abstract class TwoColumnGbsTelemetryViewModelBase : TwoColumnRttBoxViewModel, IGbsTelemetry
{
    public const string BaseId = "two-column-rtt-gbs";

    public TwoColumnGbsTelemetryViewModelBase()
        : base(DesignTime.Id.TypeId)
    {
        ItemId = DesignTime.Id.TypeId;
        GbsClient = null!;
    }

    protected TwoColumnGbsTelemetryViewModelBase(
        string id,
        IAsvGbsExClient gbsClient,
        TimeSpan? networkErrorTimeout = null
    )
        : base(id, networkErrorTimeout)
    {
        ItemId = id;
        GbsClient = gbsClient;
        Status = IGbsTelemetry.DefaultStatusColor;
    }

    public string ItemId { get; }

    protected IAsvGbsExClient GbsClient { get; }
}

public abstract class TwoColumnTelemetryItemViewModelBase<T>
    : TwoColumnRttBoxViewModel<T>,
        IGbsTelemetry
{
    public const string BaseId = TwoColumnGbsTelemetryViewModelBase.BaseId;

    public TwoColumnTelemetryItemViewModelBase()
        : base(DesignTime.Id.TypeId, Observable.Never<T>(), null)
    {
        ItemId = DesignTime.Id.TypeId;
        GbsClient = null!;
    }

    protected TwoColumnTelemetryItemViewModelBase(
        string id,
        IAsvGbsExClient gbsClient,
        Observable<T> valueStream,
        TimeSpan? networkErrorTimeout = null
    )
        : base(id, valueStream, networkErrorTimeout)
    {
        ItemId = id;
        GbsClient = gbsClient;
        Status = IGbsTelemetry.DefaultStatusColor;
    }

    public string ItemId { get; }

    protected IAsvGbsExClient GbsClient { get; }
}
