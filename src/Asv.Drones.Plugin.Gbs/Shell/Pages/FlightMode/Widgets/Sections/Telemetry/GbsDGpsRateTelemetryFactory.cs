using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsDGpsRateTelemetryFactory(ILoggerFactory loggerFactory)
    : ITelemetryItemFactory
{
    public const string Id = "gbs-dgps-rate";
    private const AsvColorKind DefaultStatusColor = AsvColorKind.Info5;
    private const ushort PreviewRate = 30;

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public IRttBoxViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var rate = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .DgpsRate.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Prepend((ushort)0);

        return InternalCreate(rate);
    }

    public IRttBoxViewModel CreatePreview()
    {
        var rate = Observable.Return(PreviewRate).Concat(Observable.Never<ushort>());

        return InternalCreate(rate);
    }

    private IRttBoxViewModel InternalCreate(Observable<ushort> rate)
    {
        return new KeyValueRttBoxViewModel<ushort>(Id, loggerFactory, rate, null)
        {
            Header = "DGpsRate",
            Icon = MaterialIconKind.Frequency,
            UpdateAction = (model, changes) =>
            {
                model[0, "DGpsRate", null].ValueString = BytesRate.ConvertToStringWithUnits(
                    changes
                );
            },
            Status = DefaultStatusColor,
        };
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
