using Asv.Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class SettingsPageRegistrations
{
    public static PagesRegistrations.Builder RegisterGbsSettings(
        this PagesRegistrations.Builder builder
    )
    {
        // Dialogs
        builder.AppBuilder.ViewLocator.RegisterViewFor<
            AddCoordsRecordDialogViewModel,
            AddCoordsRecordDialogView
        >();

        // Gbs settings group
        builder.AppBuilder.Services.AddKeyedTransient<ITreePageMenuItem, SettingsGbsGroup>(
            SettingsPageViewModel.PageId
        );

        // Saved coords settings page
        builder.AppBuilder.Settings.AddSubPage<
            SettingsSavedCoordsViewModel,
            SettingsSavedCoordsView,
            SettingsSavedCoordsTreePageMenu
        >(SettingsSavedCoordsViewModel.SubPageId);

        return builder;
    }
}
