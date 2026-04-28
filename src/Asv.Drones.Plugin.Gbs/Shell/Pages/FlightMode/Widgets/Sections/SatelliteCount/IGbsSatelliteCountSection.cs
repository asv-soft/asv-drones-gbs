using Asv.Drones.Api;
using Avalonia.Controls;

namespace Asv.Drones.Plugin.Gbs;

public interface IGbsSatelliteCountSection : IFlightWidgetSection
{
    public GridLength BeidouSats { get; }

    public GridLength GalSats { get; }

    public GridLength GlonassSats { get; }

    public GridLength GpsSats { get; }

    public GridLength ImesSats { get; }

    public GridLength QzssSats { get; }

    public GridLength SbasSats { get; }
}
