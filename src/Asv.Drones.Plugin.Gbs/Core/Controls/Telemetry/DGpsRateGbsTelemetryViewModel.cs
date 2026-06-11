using Asv.Common;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs
{
    public class DGpsRateGbsTelemetryViewModel : GbsTelemetryViewModelBase
    {
        public const string RttId = "gbs-dgps-rate";
        private const int RateValue = 30;

        public DGpsRateGbsTelemetryViewModel()
        {
            ValueString = BytesRate.ConvertToStringWithUnits(RateValue);
        }

        public DGpsRateGbsTelemetryViewModel(
            IAsvGbsExClient gbsClient,
            TimeSpan? networkErrorTimeout = null
        )
            : base(RttId, gbsClient, networkErrorTimeout)
        {
            Order = 2;
            Header = "DGpsRate";
            Icon = MaterialIconKind.Frequency;

            GbsClient
                .DgpsRate.ObserveOnUIThreadDispatcher()
                .Subscribe(rate => ValueString = BytesRate.ConvertToStringWithUnits(rate))
                .DisposeItWith(Disposable);
        }
    }
}

file abstract class BytesRate
{
    private const double OneKb = 1024.0;
    private const double OneMb = OneKb * OneKb;
    private const double OneGb = OneMb * OneKb;

    public static string GetUnit(double bytesPerSec)
    {
        return bytesPerSec switch
        {
            double.NaN or < 0 => string.Empty,
            (<= OneKb) => "b/s",
            (>= OneKb) and < OneMb => "kb/s",
            (>= OneMb) and < OneGb => "mb/s",
            (>= OneGb) => "g/s",
        };
    }

    public static string ConvertToString(double bytesPerSec)
    {
        return bytesPerSec switch
        {
            double.NaN or < 0 => Asv.Avalonia.RS.Not_Available,
            0 => $"{bytesPerSec, -4:F0}",
            (< 1) => $"{bytesPerSec, -4:F3}",
            (< OneKb) => $"{bytesPerSec, -4:F0}",
            (>= OneKb) and < OneMb or >= OneMb and < OneGb => $"{bytesPerSec / OneKb, -4:F0}",
            (>= OneGb) => $"{bytesPerSec / OneMb, -4:F0}",
        };
    }

    public static string ConvertToStringWithUnits(double bytesPerSec)
    {
        return $"{ConvertToString(bytesPerSec)} {GetUnit(bytesPerSec)}";
    }
}
