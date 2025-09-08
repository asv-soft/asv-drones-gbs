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

public class GbsDevicePageViewModel : DevicePageViewModel<IGbsPage, GbsPageViewModelConfig>, IGbsPage
{
    public const string PageId = "gbs";
    
    public GbsDevicePageViewModel()
        : this(NullDeviceManager.Instance, NullCommandService.Instance, DesignTime.ContainerHost,
            DesignTime.Configuration, DesignTime.LoggerFactory)
    {
        
    }

    [ImportingConstructor]
    public GbsDevicePageViewModel(IDeviceManager devices, ICommandService cmd, IContainerHost containerHost,
        IConfiguration cfg, ILoggerFactory loggerFactory)
        : base(PageId, devices, cmd, cfg, loggerFactory)
    {
        
    }

    public override IEnumerable<IRoutable> GetRoutableChildren()
    {
        return [];
    }

    protected override void AfterLoadExtensions()
    {
        // do nothing
    }

    public override IExportInfo Source => GbsModule.Instance;
    
    protected override void AfterDeviceInitialized(IClientDevice device, CancellationToken onDisconnectedToken)
    {
        // do nothing
    }
}

public class GbsPageViewModelConfig : PageConfig
{
}