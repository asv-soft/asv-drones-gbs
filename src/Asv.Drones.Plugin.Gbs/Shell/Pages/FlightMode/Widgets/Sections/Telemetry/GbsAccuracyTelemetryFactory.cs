using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsAccuracyTelemetryFactory(
    IUnitService unitService,
    ILoggerFactory loggerFactory
) : ITelemetryItemFactory
{
    public const string Id = "gbs-accuracy";
    private const AsvColorKind DefaultStatusColor = AsvColorKind.Info5;

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public IRttBoxViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var accuracy = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .AccuracyMeter.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Prepend(double.NaN);

        return InternalCreate(accuracy);
    }

    public IRttBoxViewModel CreatePreview()
    {
        var accuracy = Observable.Return(5d).Concat(Observable.Never<double>());

        return InternalCreate(accuracy);
    }

    private IRttBoxViewModel InternalCreate(Observable<double> accuracy)
    {
        return new SplitDigitRttBoxViewModel(
            Id,
            loggerFactory,
            unitService,
            DistanceUnit.Id,
            accuracy,
            null
        )
        {
            Header = "Accuracy",
            Icon = MaterialIconKind.CrosshairsGps,
            Status = DefaultStatusColor,
            FormatString = "F2",
        };
    }
}
