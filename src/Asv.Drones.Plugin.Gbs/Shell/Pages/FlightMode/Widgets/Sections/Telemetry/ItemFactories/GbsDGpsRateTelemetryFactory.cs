using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsDGpsRateTelemetryFactory : ITelemetryItemFactory
{
    public const string Id = "gbs-dgps-rate";

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITileViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var rate = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .DgpsRate.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Prepend((ushort)0)
            .ObserveOnUIThreadDispatcher();

        return new TelemetryViewModel<ushort>(Id, rate, Update)
        {
            Density = TileDensity.Inline,
            Header = RS.GbsDGpsRateTelemetry_Header,
            ShortHeader = RS.GbsDGpsRateTelemetry_ShortHeader,
            Icon = MaterialIconKind.Frequency,
        };

        static void Update(TelemetryViewModel<ushort> tile, ushort changes)
        {
            tile.Text = BytesRate.ConvertToString(changes);
            tile.Units = BytesRate.GetUnit(changes);
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
