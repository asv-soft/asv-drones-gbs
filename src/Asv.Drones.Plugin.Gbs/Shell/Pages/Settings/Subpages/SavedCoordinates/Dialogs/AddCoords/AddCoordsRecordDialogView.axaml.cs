using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Asv.Drones.Plugin.Gbs;

[ExportViewFor<AddCoordsRecordDialogViewModel>]
public partial class AddCoordsRecordDialogView : UserControl
{
    public AddCoordsRecordDialogView()
    {
        InitializeComponent();
    }
}
