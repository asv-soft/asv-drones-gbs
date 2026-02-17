using Asv.Avalonia;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Asv.Drones.Plugin.Gbs;

[ExportViewFor<GbsDevicePageViewModel>]
public partial class GbsDevicePageView : UserControl
{
    public GbsDevicePageView()
    {
        InitializeComponent();
    }
}
