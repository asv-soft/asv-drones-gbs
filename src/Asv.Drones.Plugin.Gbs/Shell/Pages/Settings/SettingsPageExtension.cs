using System.Composition;
using Asv.Avalonia;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

[ExportExtensionFor<ISettingsPage>]
[method: ImportingConstructor]
public class SettingsPageExtension(ILoggerFactory loggerFactory) : IExtensionFor<ISettingsPage>
{
    public void Extend(ISettingsPage context, CompositeDisposable contextDispose)
    {
        var gbsSettingsGroup = new TreePage(
            "gbs",
            RS.SettingsPageExtension_Group_Gbs_Title,
            GbsModule.DefaultIcon,
            NavigationId.Empty,
            NavigationId.Empty,
            loggerFactory
        ).DisposeItWith(contextDispose);
        context.Nodes.Add(gbsSettingsGroup);

        context.Nodes.Add(
            new TreePage(
                SettingsSavedCoordsViewModel.SubPageId,
                RS.SettingsPageExtension_Group_Gbs_FixedModeCoords_Title,
                SettingsSavedCoordsViewModel.Icon,
                SettingsSavedCoordsViewModel.SubPageId,
                gbsSettingsGroup.Id,
                loggerFactory
            ).DisposeItWith(contextDispose)
        );
    }
}
