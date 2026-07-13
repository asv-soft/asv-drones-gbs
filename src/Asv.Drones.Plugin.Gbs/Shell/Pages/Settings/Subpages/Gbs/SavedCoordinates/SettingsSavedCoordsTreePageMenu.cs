using Asv.Avalonia;
using Asv.Modeling;

namespace Asv.Drones.Plugin.Gbs;

public class SettingsSavedCoordsTreePageMenu()
    : TreePageMenuItem(
        SettingsSavedCoordsViewModel.SubPageId,
        RS.SettingsPage_Group_Gbs_FixedModeCoords_Title,
        SettingsSavedCoordsViewModel.Icon,
        new NavId(SettingsSavedCoordsViewModel.SubPageId),
        new NavId(SettingsGbsGroup.GroupId)
    ) { }
