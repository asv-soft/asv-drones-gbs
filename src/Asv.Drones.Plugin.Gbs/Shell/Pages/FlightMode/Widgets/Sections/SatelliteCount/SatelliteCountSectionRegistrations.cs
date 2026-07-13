using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class SatelliteCountSectionRegistrations
{
    extension(SectionsRegistrations.Builder builder)
    {
        public SectionsRegistrations.Builder RegisterSatelliteCountSection()
        {
            builder.AppBuilder.Extensions.Register<
                IGbsFlightWidget,
                GbsFlightWidgetSatelliteCountSectionExtension
            >();
            builder.AppBuilder.ViewLocator.RegisterViewFor<
                IGbsSatelliteCountSection,
                GbsSatelliteCountSectionView
            >();
            builder.AppBuilder.ViewModel.RegisterWithArgs<
                IGbsSatelliteCountSection,
                GbsSatelliteCountSectionViewModel,
                GbsSatelliteCountSectionArgs
            >();
            return builder;
        }
    }
}
