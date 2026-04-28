using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class CommandsMixin
{
    public static CoreMixin.Builder RegisterCommands(this CoreMixin.Builder builder)
    {
        builder.GbsPlugin.AppBuilder.Commands.Register<OpenGbsPageCommand>();

        return builder;
    }
}
