using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class BaseStationModeGbsTelemetryViewModel : GbsTelemetryViewModelBase
{
    public const string RttId = "gbs-base-station-mode";

    public BaseStationModeGbsTelemetryViewModel()
    {
        ValueString = "Idle";
    }

    public BaseStationModeGbsTelemetryViewModel(
        IAsvGbsExClient gbsClient,
        TimeSpan? networkErrorTimeout = null
    )
        : base(RttId, gbsClient, networkErrorTimeout)
    {
        Order = 2;
        Header = "Mode";
        Icon = MaterialIconKind.StateMachine;

        GbsClient
            .CustomMode.ObserveOnUIThreadDispatcher()
            .Subscribe(mode =>
                ValueString = mode.ToString().Replace(nameof(AsvGbsCustomMode), string.Empty)
            )
            .DisposeItWith(Disposable);
    }
}
