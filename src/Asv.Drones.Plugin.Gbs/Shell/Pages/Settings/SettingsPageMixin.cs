using Asv.Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public static class SettingsPageMixin
{
    public static PagesMixin.Builder RegisterGbsSettings(this PagesMixin.Builder builder)
    {
        var appBuilder = builder.Shell.GbsPlugin.AppBuilder;

        // Dialogs
        appBuilder.ViewLocator.RegisterViewFor<
            AddCoordsRecordDialogViewModel,
            AddCoordsRecordDialogView
        >();

        // Gbs settings group
        appBuilder.Services.AddKeyedTransient<ITreePage, SettingsGbsGroup>(
            SettingsPageViewModel.PageId
        );

        // Saved coords settings page
        appBuilder.Shell.Pages.Settings.AddSubPage<
            SettingsSavedCoordsViewModel,
            SettingsSavedCoordsView,
            SettingsSavedCoordsTreePageMenu
        >(SettingsSavedCoordsViewModel.SubPageId);

        return builder;
    }
}
