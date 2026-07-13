using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsPageRegistrations
{
    public static PagesRegistrations.Builder RegisterGbsPage(
        this PagesRegistrations.Builder builder
    )
    {
        var pages = builder.AppBuilder.Shell.Pages;

        pages.Register<GbsDevicePageViewModel, GbsDevicePageView>(GbsDevicePageViewModel.PageId);
        pages.Home.UseItemExtension<HomePageGbsDeviceItemAction>();

        return builder;
    }
}
