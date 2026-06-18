using System.Globalization;
using Asv.Avalonia;
using Asv.Common;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs
{
    public class LinkQualityGbsTelemetryViewModel : TwoColumnGbsTelemetryViewModelBase
    {
        public const string RttId = "gbs-link-quality";

        public LinkQualityGbsTelemetryViewModel()
        {
            Header = "Link Quality";
            Left.ValueString = "0.3";
            Right.ValueString = "30";
            Right.UnitSymbol = "%";
        }

        public LinkQualityGbsTelemetryViewModel(
            IHeartbeatClient heartbeat,
            IAsvGbsExClient gbsClient,
            IUnitService unitService,
            TimeSpan? networkErrorTimeout = null
        )
            : base(RttId, gbsClient, networkErrorTimeout)
        {
            Order = 1;
            Header = "Link Quality";
            Icon = MaterialIconKind.Wifi;
            var progress = unitService.GetRequiredUnitOfType<ProgressUnit>(ProgressUnit.Id);

            var normalized = progress.AvailableUnits[ProgressNormalizedUnitItem.Id];
            var percent = progress.AvailableUnits[ProgressPercentUnitItem.Id];

            heartbeat
                .LinkQuality.ObserveOnUIThreadDispatcher()
                .Subscribe(q =>
                {
                    Left.ValueString = normalized.Print(q, "F2");

                    Right.ValueString = percent.PrintFromSi(q * 100, "F0");
                    Right.UnitSymbol = percent.Symbol;
                })
                .DisposeItWith(Disposable);
        }
    }
}
