using Asv.Avalonia;
using Asv.Modeling;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

public class SettingsSavedCoordsTreePageMenu : TreePage
{
    public SettingsSavedCoordsTreePageMenu(ILoggerFactory loggerFactory)
        : base(
            SettingsSavedCoordsViewModel.SubPageId,
            RS.SettingsPage_Group_Gbs_FixedModeCoords_Title,
            SettingsSavedCoordsViewModel.Icon,
            new NavId(SettingsSavedCoordsViewModel.SubPageId),
            new NavId(SettingsGbsGroup.GroupId),
            loggerFactory
        ) { }
}
