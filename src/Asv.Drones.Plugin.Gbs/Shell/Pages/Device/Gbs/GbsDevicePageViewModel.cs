using System.Composition;
using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.IO;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

public class GbsPageViewModelConfig { }

[ExportPage(PageId)]
public class GbsDevicePageViewModel : DevicePageViewModel<GbsDevicePageViewModel>, IGbsPage
{
    public const string PageId = "gbs";

    public GbsDevicePageViewModel()
        : this(
            NullDeviceManager.Instance,
            DesignTime.CommandService,
            NullLayoutService.Instance,
            DesignTime.LoggerFactory,
            DesignTime.DialogService
        ) { }

    [ImportingConstructor]
    public GbsDevicePageViewModel(
        IDeviceManager devices,
        ICommandService cmd,
        ILayoutService layoutService,
        ILoggerFactory loggerFactory,
        IDialogService dialogService
    )
        : base(PageId, devices, cmd, layoutService, loggerFactory, dialogService) { }

    public override IEnumerable<IRoutable> GetChildren()
    {
        return [];
    }

    protected override void AfterLoadExtensions() { }

    protected override void AfterDeviceInitialized(
        IClientDevice device,
        CancellationToken onDisconnectedToken
    ) { }

    public override IExportInfo Source => GbsModule.Instance;
}
