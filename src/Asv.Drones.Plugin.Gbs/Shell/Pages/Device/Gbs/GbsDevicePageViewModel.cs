using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.IO;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

public class GbsPageViewModelConfig { }

public class GbsDevicePageViewModel : DevicePageViewModel<GbsDevicePageViewModel>, IGbsPage
{
    public const string PageId = "gbs";

    public GbsDevicePageViewModel()
        : this(
            NullDeviceManager.Instance,
            DesignTime.CommandService,
            NullLayoutService.Instance,
            DesignTime.LoggerFactory,
            DesignTime.DialogService,
            DesignTime.ExtensionService
        ) { }

    public GbsDevicePageViewModel(
        IDeviceManager devices,
        ICommandService cmd,
        ILayoutService layoutService,
        ILoggerFactory loggerFactory,
        IDialogService dialogService,
        IExtensionService ext
    )
        : base(PageId, devices, cmd, layoutService, loggerFactory, dialogService, ext) { }

    public override IEnumerable<IRoutable> GetChildren()
    {
        return [];
    }

    protected override void AfterLoadExtensions() { }

    protected override void AfterDeviceInitialized(
        IClientDevice device,
        CancellationToken onDisconnectedToken
    ) { }
}
