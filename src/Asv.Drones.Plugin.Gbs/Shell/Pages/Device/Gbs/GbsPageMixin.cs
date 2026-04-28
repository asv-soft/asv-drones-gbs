using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsPageMixin
{
    public static PagesMixin.Builder RegisterGbsPage(this PagesMixin.Builder builder)
    {
        var pages = builder.Shell.GbsPlugin.AppBuilder.Shell.Pages;

        pages.Register<GbsDevicePageViewModel, GbsDevicePageView>(GbsDevicePageViewModel.PageId);
        pages.Home.UseItemExtension<HomePageGbsDeviceItemAction>();

        return builder;
    }
}
