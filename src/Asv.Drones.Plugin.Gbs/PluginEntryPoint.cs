using Asv.Avalonia.Plugins;
using Microsoft.Extensions.Hosting;

namespace Asv.Drones.Plugin.Gbs;

public class PluginEntryPoint : IPluginAppBuilder
{
    public void Register(IHostApplicationBuilder builder)
    {
        builder.RegisterGbsPlugin();
    }
}
