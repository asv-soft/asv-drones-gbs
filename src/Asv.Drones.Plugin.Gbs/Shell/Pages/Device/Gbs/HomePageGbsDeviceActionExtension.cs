using System.Composition;
using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.IO;
using Asv.Mavlink;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

[ExportExtensionFor<IHomePageItem>]
[method: ImportingConstructor]
public class HomePageGbsDeviceActionExtension(ILoggerFactory loggerFactory)
    : HomePageDeviceItemAction
{
    protected override IActionViewModel? TryCreateAction(
        IClientDevice device,
        HomePageDeviceItem context
    )
    {
        if (device.GetMicroservice<IAsvGbsExClient>() == null)
        {
            return null;
        }

        return new ActionViewModel(GbsDevicePageViewModel.PageId, loggerFactory)
        {
            Icon = GbsModule.DefaultIcon,
            Header = "Gbs control",
            Description = "Ground base station device control",
            Command = new BindableAsyncCommand(OpenGbsPageCommand.Id, context),
            CommandParameter = DevicePageViewModelMixin.CreateOpenPageArgs(device.Id),
        };
    }
}
