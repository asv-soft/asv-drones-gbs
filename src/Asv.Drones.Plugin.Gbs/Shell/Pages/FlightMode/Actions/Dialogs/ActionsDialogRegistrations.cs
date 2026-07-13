using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class ActionsDialogRegistrations
{
    public static ActionsRegistrations.Builder RegisterDialogs(
        this ActionsRegistrations.Builder builder
    )
    {
        builder.AppBuilder.ViewLocator.RegisterViewFor<
            AutoModeDialogViewModel,
            AutoModeDialogView
        >();
        builder.AppBuilder.ViewLocator.RegisterViewFor<
            SetCoordsNameDialogViewModel,
            SetCoordsNameDialogView
        >();
        builder.AppBuilder.ViewLocator.RegisterViewFor<
            FixedModeDialogViewModel,
            FixedModeDialogView
        >();

        return builder;
    }
}
