using Asv.Avalonia;
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