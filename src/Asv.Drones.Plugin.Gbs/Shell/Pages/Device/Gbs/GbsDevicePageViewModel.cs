using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.Common;
using Asv.IO;
using Asv.Modeling;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class GbsPageViewModelConfig { }

public class GbsDevicePageViewModel : DevicePageViewModel<GbsDevicePageViewModel>, IGbsPage
{
    public const string PageId = "gbs";

    public GbsDevicePageViewModel()
        : this(
            DesignTime.PageContext,
            NullDeviceManager.Instance,
            DesignTime.LoggerFactory,
            DesignTime.DialogService,
            DesignTime.ExtensionService
        ) { }

    public GbsDevicePageViewModel(
        IPageContext context,
        IDeviceManager devices,
        ILoggerFactory loggerFactory,
        IDialogService dialogService,
        IExtensionService ext
    )
        : base(PageId, context, devices, loggerFactory, dialogService, ext)
    {
        Target
            .Where(w => w.HasValue)
            .Select(w => w!.Value)
            .ObserveOnUIThreadDispatcher()
            .Subscribe(w => OnDeviceConnected(w.Device, w.WhenDisconnectedToken))
            .DisposeItWith(Disposable);
    }

    public override IEnumerable<IViewModel> GetChildren()
    {
        return [];
    }

    protected override void AfterLoadExtensions() { }

    private void OnDeviceConnected(IClientDevice device, CancellationToken onDisconnectedToken) { }
}
