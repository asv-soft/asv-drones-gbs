using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.IO;
using Asv.Mavlink;
using Asv.Modeling;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class HomePageGbsDeviceItemAction : HomePageDeviceItemAction
{
    public const string StaticId = "ext.home.device-action.gbs";

    public override string Id => StaticId;

    protected override IActionViewModel? TryCreateAction(
        IClientDevice device,
        HomePageDeviceItem context
    )
    {
        if (device.GetMicroservice<IAsvGbsExClient>() == null)
        {
            return null;
        }

        return new ActionViewModel(GbsDevicePageViewModel.PageId)
        {
            Icon = GbsPluginRegistrations.DefaultIcon,
            Header = "Gbs control",
            Description = "Ground base station device control",
            Command = new ReactiveCommand(
                async (_, _) =>
                    await context.GoTo(
                        new NavPath(
                            new NavId(
                                GbsDevicePageViewModel.PageId,
                                DevicePageViewModelMixin.CreateOpenPageArgs(device.Id)
                            )
                        )
                    )
            ),
        };
    }
}
