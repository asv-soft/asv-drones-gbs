using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Material.Icons;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsVisibleSatellitesTelemetryFactory(ILoggerFactory loggerFactory)
    : ITelemetryItemFactory
{
    public const string Id = "gbs-visible-satellites";
    private const AsvColorKind DefaultStatusColor = AsvColorKind.Info5;

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public IRttBoxViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var satellites = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .AllSatellites.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Select(value => value)
            .Prepend((byte)0);

        return InternalCreate(satellites);
    }

    public IRttBoxViewModel CreatePreview()
    {
        var satellites = Observable.Return((byte)10).Concat(Observable.Never<byte>());

        return InternalCreate(satellites);
    }

    private IRttBoxViewModel InternalCreate(Observable<byte> satellites)
    {
        return new KeyValueRttBoxViewModel<byte>(Id, loggerFactory, satellites, null)
        {
            Header = "All Satellites",
            Icon = MaterialIconKind.SatelliteVariant,
            UpdateAction = (model, changes) =>
            {
                model[0, "All Satellites", null].ValueString = changes.ToString();
            },
            Status = DefaultStatusColor,
        };
    }
}
