using Asv.Avalonia;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

public class SettingsGbsGroup : TreePage
{
    public const string GroupId = "gbs";

    public SettingsGbsGroup(ILoggerFactory loggerFactory)
        : base(
            GroupId,
            RS.SettingsPage_Group_Gbs_Title,
            GbsPluginMixin.DefaultIcon,
            NavigationId.Empty,
            NavigationId.Empty,
            loggerFactory
        ) { }
}
