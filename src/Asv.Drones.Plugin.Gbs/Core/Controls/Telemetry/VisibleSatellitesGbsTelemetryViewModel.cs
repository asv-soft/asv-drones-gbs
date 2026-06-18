using Asv.Avalonia;
using Asv.Common;
using Asv.Mavlink;
using Material.Icons;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class VisibleSatellitesGbsTelemetryViewModel : GbsTelemetryViewModelBase
{
    public const string RttId = "gbs-visible-satellites";

    public VisibleSatellitesGbsTelemetryViewModel()
    {
        DesignTime.ThrowIfNotDesignMode();
        ValueString = "10";
    }

    public VisibleSatellitesGbsTelemetryViewModel(
        IAsvGbsExClient gbsClient,
        TimeSpan? networkErrorTimeout = null
    )
        : base(RttId, gbsClient, networkErrorTimeout)
    {
        Order = 1;
        Header = "All Satellites";
        Icon = MaterialIconKind.SatelliteVariant;

        GbsClient
            .AllSatellites.ObserveOnUIThreadDispatcher()
            .Subscribe(count => ValueString = count.ToString())
            .DisposeItWith(Disposable);
    }
}
