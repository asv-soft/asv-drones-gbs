using System.Composition;
using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.Cfg;
using Asv.IO;
using Microsoft.Extensions.Logging;

namespace Asv.Drones.Plugin.Gbs;

public interface IGbsPage : IDevicePage
{
    
}

public class GbsDevicePageViewModel : DevicePageViewModel<GbsDevicePageViewModel>, IGbsPage
{
    public const string PageId = "gbs";
    
    public GbsDevicePageViewModel()
        : this(NullDeviceManager.Instance, DesignTime.CommandService, NullLayoutService.Instance, DesignTime.Configuration,
            DesignTime.LoggerFactory, DesignTime.DialogService)
    {
        
    }

    [ImportingConstructor]
    public GbsDevicePageViewModel(IDeviceManager devices, ICommandService cmd,  ILayoutService layoutService,
        IConfiguration cfg, ILoggerFactory loggerFactory, IDialogService dialogService)
        : base(PageId, devices, cmd, layoutService, loggerFactory, dialogService)
    {
    }


    public override IEnumerable<IRoutable> GetChildren()
    {
        throw new NotImplementedException();
    }

    protected override void AfterLoadExtensions()
    {
        throw new NotImplementedException();
    }

    public override IExportInfo Source => GbsModule.Instance;
    protected override void AfterDeviceInitialized(IClientDevice device, CancellationToken onDisconnectedToken)
    {
        throw new NotImplementedException();
    }
}

public class GbsPageViewModelConfig
{
}