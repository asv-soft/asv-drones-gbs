using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Avalonia.IO;
using Asv.Avalonia.Plugins;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Api;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.Drones.Plugin.Gbs.Preview;

public partial class App : Application
{
    public App()
    {
        var conventions = new ConventionBuilder();
        var containerCfg = new ContainerConfiguration();
        containerCfg
            .WithExport(NullContainerHost.Instance)
            .WithExport<IConfiguration>(new InMemoryConfiguration())
            .WithExport(NullLoggerFactory.Instance)
            .WithExport(NullAppPath.Instance)
            .WithExport(NullPluginManager.Instance)
            .WithExport(NullLogReaderService.Instance)
            .WithExport(NullAppInfo.Instance)
            .WithExport<IDataTemplateHost>(this)
            .WithExport<IMeterFactory>(new DefaultMeterFactory())
            .WithExport(TimeProvider.System)
            .WithDefaultConventions(conventions)
            .WithAssemblies(DefaultAssemblies.Distinct());
        var container = containerCfg.CreateContainer();
        DataTemplates.Add(new CompositionViewLocator(container));
    }
    
    private IEnumerable<Assembly> DefaultAssemblies
    {
        get
        {
            yield return typeof(GbsModule).Assembly; // Asv.Drones.Plugin.Gbs
            yield return typeof(ApiModule).Assembly; // Asv.Drones.Api
            yield return typeof(AppHost).Assembly; // Asv.Avalonia
            yield return typeof(DeviceManager).Assembly; // Asv.Avalonia.IO
            yield return typeof(PluginManagerModule).Assembly; // Asv.Avalonia.Plugins
            yield return typeof(GeoMapModule).Assembly; // Asv.Avalonia.GeoMap
        }
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}