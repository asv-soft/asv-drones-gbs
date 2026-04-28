using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class DialogsMixin
{
    public static GbsWidgetMixin.Builder RegisterDialogs(this GbsWidgetMixin.Builder builder)
    {
        builder.Widgets.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            AutoModeDialogViewModel,
            AutoModeDialogView
        >();
        builder.Widgets.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            SetCoordsNameDialogViewModel,
            SetCoordsNameDialogView
        >();
        builder.Widgets.FlightMode.Pages.Shell.GbsPlugin.AppBuilder.ViewLocator.RegisterViewFor<
            FixedModeDialogViewModel,
            FixedModeDialogView
        >();

        return builder;
    }
}
