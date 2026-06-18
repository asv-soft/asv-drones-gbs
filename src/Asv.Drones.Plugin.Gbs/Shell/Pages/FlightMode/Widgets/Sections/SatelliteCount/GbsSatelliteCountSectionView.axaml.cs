using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Asv.Drones.Plugin.Gbs;

public partial class GbsSatelliteCountSectionView : UserControl
{
    public GbsSatelliteCountSectionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
