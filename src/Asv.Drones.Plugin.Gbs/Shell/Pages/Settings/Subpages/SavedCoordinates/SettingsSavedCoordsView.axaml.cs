using Asv.Avalonia;
using Avalonia.Controls;

namespace Asv.Drones.Plugin.Gbs;

[ExportViewFor(typeof(SettingsSavedCoordsViewModel))]
public partial class SettingsSavedCoordsView : UserControl
{
    public SettingsSavedCoordsView()
    {
        InitializeComponent();
    }
}
