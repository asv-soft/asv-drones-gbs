using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsBaseStationModeTelemetryFactory(ILoggerFactory loggerFactory)
    : ITelemetryItemFactory
{
    public const string Id = "gbs-base-station-mode";
    private const AsvColorKind DefaultStatusColor = AsvColorKind.Info5;

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public IRttBoxViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var mode = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .CustomMode.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Select(ModeToString)
            .Prepend(string.Empty);

        return InternalCreate(mode);
    }

    public IRttBoxViewModel CreatePreview()
    {
        var mode = Observable.Return("Idle").Concat(Observable.Never<string>());

        return InternalCreate(mode);
    }

    private IRttBoxViewModel InternalCreate(Observable<string> mode)
    {
        return new KeyValueRttBoxViewModel<string>(Id, loggerFactory, mode, null)
        {
            Header = "Mode",
            Icon = MaterialIconKind.StateMachine,
            UpdateAction = (model, changes) =>
            {
                model[0, "Mode", null].ValueString = changes;
            },
            Status = DefaultStatusColor,
        };
    }

    private static string ModeToString(AsvGbsCustomMode mode) =>
        mode.ToString().Replace(nameof(AsvGbsCustomMode), string.Empty);
}
