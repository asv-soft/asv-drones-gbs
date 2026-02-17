using System.Composition;
using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

[Export(typeof(IAsyncCommand))]
[Shared]
[method: ImportingConstructor]
public class OpenGbsPageCommand(INavigationService nav)
    : OpenPageCommandBase(GbsDevicePageViewModel.PageId, nav)
{
    public const string Id = $"{BaseId}.open.{GbsDevicePageViewModel.PageId}";

    public static readonly ICommandInfo StaticInfo = new CommandInfo
    {
        Id = Id,
        Name = "Open GBS control page",
        Description = "Command that opens GBS control page",
        Icon = GbsModule.DefaultIcon,
        Source = GbsModule.Instance,
        DefaultHotKey = null,
    };

    public override ICommandInfo Info => StaticInfo;
}
