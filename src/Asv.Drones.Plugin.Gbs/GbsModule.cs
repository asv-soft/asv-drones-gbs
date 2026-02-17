using System.Composition.Hosting;
using Asv.Avalonia;
using Asv.Avalonia.IO;
using Avalonia.Controls;
using Material.Icons;

namespace Asv.Drones.Plugin.Gbs;

public class GbsModule : IExportInfo
{
    public const MaterialIconKind DefaultIcon = MaterialIconKind.Radio;
    public const string Name = "Gbs";
    public static IExportInfo Instance { get; } = new GbsModule();

    private GbsModule() { }

    public string ModuleName => Name;
}

public static class ContainerConfigurationMixin
{
    public static ContainerConfiguration WithDependenciesFromGbsPlugin(
        this ContainerConfiguration containerConfiguration
    )
    {
        var assembly = typeof(IoModule).Assembly;
        if (Design.IsDesignMode)
        {
            return containerConfiguration.WithAssemblies([assembly]);
        }

        var exceptionTypes = new List<Type>();
        var gbsTypes = assembly.GetTypes().Except(exceptionTypes);

        return containerConfiguration.WithParts(gbsTypes);
    }
}
