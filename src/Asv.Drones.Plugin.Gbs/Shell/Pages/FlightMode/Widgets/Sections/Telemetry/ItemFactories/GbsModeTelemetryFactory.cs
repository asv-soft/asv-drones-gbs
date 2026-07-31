using Asv.Avalonia;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsModeTelemetryFactory : ITelemetryItemFactory
{
    public const string Id = "gbs-mode";

    public string ItemId => Id;

    public bool CanCreate(in IClientDevice device) =>
        device.GetMicroservice<IAsvGbsExClient>() is not null;

    public ITileViewModel Create(in IClientDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var mode = device
            .GetRequiredMicroservice<IAsvGbsExClient>()
            .CustomMode.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .Select(ModeToString)
            .Prepend(string.Empty)
            .ObserveOnUIThreadDispatcher();

        return new TelemetryViewModel<string>(
            Id,
            mode,
            static (tile, changes) => tile.Text = changes
        )
        {
            Density = TileDensity.Inline,
            Header = RS.GbsModeTelemetry_Header,
            ShortHeader = RS.GbsModeTelemetry_ShortHeader,
            Icon = MaterialIconKind.StateMachine,
        };
    }

    private static string ModeToString(AsvGbsCustomMode mode) =>
        mode.ToString().Replace(nameof(AsvGbsCustomMode), string.Empty);
}
