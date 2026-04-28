using Asv.Avalonia;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

public class SettingsSavedCoordsTreePageMenu : TreePage
{
    public SettingsSavedCoordsTreePageMenu(ILoggerFactory loggerFactory)
        : base(
            SettingsSavedCoordsViewModel.SubPageId,
            RS.SettingsPage_Group_Gbs_FixedModeCoords_Title,
            SettingsSavedCoordsViewModel.Icon,
            SettingsSavedCoordsViewModel.SubPageId,
            SettingsGbsGroup.GroupId,
            loggerFactory
        ) { }
}
