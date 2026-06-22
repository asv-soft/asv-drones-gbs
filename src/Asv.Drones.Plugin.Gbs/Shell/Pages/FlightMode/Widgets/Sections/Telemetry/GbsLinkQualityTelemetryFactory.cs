using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsLinkQualityTelemetryFactory(IUnitService unitService) : ITelemetryItemFactory
{
    public const string Id = "gbs-link-quality";
    private const AsvColorKind DefaultStatusColor = AsvColorKind.Info5;

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null
        && device.GetMicroservice<IHeartbeatClient>() is not null;

    public IRttBoxViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var linkQuality = device
            .GetRequiredMicroservice<IHeartbeatClient>()
            .LinkQuality.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Prepend(double.NaN);

        return InternalCreate(linkQuality);
    }

    public IRttBoxViewModel CreatePreview()
    {
        var linkQuality = Observable.Return(0.3d).Concat(Observable.Never<double>());

        return InternalCreate(linkQuality);
    }

    private IRttBoxViewModel InternalCreate(Observable<double> linkQuality)
    {
        var progress = unitService.GetRequiredUnitOfType<ProgressUnit>(ProgressUnit.Id);
        var normalized = progress.AvailableUnits[ProgressNormalizedUnitItem.Id];
        var percent = progress.AvailableUnits[ProgressPercentUnitItem.Id];
        var data = linkQuality.Select(value => new LinkQualityData(value, normalized, percent));

        var rtt = new TwoColumnRttBoxViewModel<LinkQualityData>(Id, data, null)
        {
            Header = "Link Quality",
            Icon = MaterialIconKind.Wifi,
            UpdateAction = (model, changes) =>
            {
                model.Left.ValueString = changes.Normalized.Print(changes.Value, "F2");
                model.Right.ValueString = changes.Percent.PrintFromSi(changes.Value * 100, "F0");
                model.Right.UnitSymbol = changes.Percent.Symbol;
            },
            Status = DefaultStatusColor,
        };

        rtt.Right.UnitSymbol = percent.Symbol;

        return rtt;
    }

#pragma warning disable SA1313
    private readonly record struct LinkQualityData(
        double Value,
        IUnitItem Normalized,
        IUnitItem Percent
    );
#pragma warning restore SA1313
}
