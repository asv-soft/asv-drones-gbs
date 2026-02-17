using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Avalonia.IO;
using Asv.Drones.Api;
using Avalonia.Markup.Xaml;

namespace Asv.Drones.Plugin.Gbs.Preview;

public partial class App : ShellHost
{
    public App()
        : base(cfg =>
        {
            cfg.WithDependenciesFromSystemModule();
            cfg.WithDependenciesFromIoModule();
            cfg.WithDependenciesFromGeoMapModule();
            cfg.WithDependenciesFromApi();
            cfg.WithDependenciesFromGbsPlugin();
        }) { }

    protected override void LoadXaml()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
