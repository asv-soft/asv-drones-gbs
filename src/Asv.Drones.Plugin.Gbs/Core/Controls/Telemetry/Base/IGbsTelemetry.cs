using Asv.Avalonia;
using Asv.Drones.Api;

namespace Asv.Drones.Plugin.Gbs;

/// <summary>
/// Represents an item in the GbsRtt system.
/// </summary>
public interface IGbsTelemetry : ITelemetryItem
{
    public const AsvColorKind DefaultStatusColor = AsvColorKind.Info5;
}
