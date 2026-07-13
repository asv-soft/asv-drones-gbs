using Asv.Avalonia;
using Asv.Modeling;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

public class SettingsGbsGroup()
    : TreePageMenuItem(
        GroupId,
        RS.SettingsPage_Group_Gbs_Title,
        GbsPluginRegistrations.DefaultIcon,
        NavId.Empty,
        NavId.Empty
    )
{
    public const string GroupId = "gbs";
}
