using System;
using System.Threading.Tasks;
using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Avalonia.IO;
using Asv.Avalonia.Plugins;
using Asv.Drones.Api;
using Avalonia;
using Avalonia.Controls;

namespace Asv.Drones.Plugin.Gbs.Preview;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
            AppHost.Instance.StopAsync().GetAwaiter().GetResult();
            Task.Factory.StartNew(AppHost.Instance.Dispose).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            AppHost.HandleApplicationCrash(e);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions { OverlayPopups = true }) // Windows
            .With(new X11PlatformOptions { OverlayPopups = true, UseDBusFilePicker = false }) // Unix/Linux
            .With(new AvaloniaNativePlatformOptions { OverlayPopups = true }) // Mac
            .LogToTrace()
            .UseAsv(builder =>
            {
                builder
                    .UseDefault()
                    .UseAppInfo(configure => configure.FillFromAssembly(typeof(App).Assembly))
                    .UseOptionalLogViewer()
                    .UseModulePlugins(configure =>
                        configure.WithApiPackage(typeof(MavlinkHost).Assembly)
                    )
                    .UseDesktopShell()
                    .UseModuleGeoMap()
                    .UseModuleIo()
                    .UseGbsPlugin();
            });
    }
}
