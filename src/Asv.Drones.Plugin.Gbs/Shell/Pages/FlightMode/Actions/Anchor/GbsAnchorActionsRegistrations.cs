using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class GbsAnchorActionsRegistrations
{
    extension(ActionsRegistrations.Builder builder)
    {
        public ActionsRegistrations.Builder RegisterGbsAnchorActions()
        {
            builder.AppBuilder.Extensions.Register<GbsAnchor, GbsAutoModeAction<GbsAnchor>>();
            builder.AppBuilder.Extensions.Register<GbsAnchor, GbsFixedModeAction<GbsAnchor>>();
            builder.AppBuilder.Extensions.Register<GbsAnchor, GbsIdleModeAction<GbsAnchor>>();
            builder.AppBuilder.Extensions.Register<GbsAnchor, GbsCancelModeAction<GbsAnchor>>();
            builder.AppBuilder.Extensions.Register<
                GbsAnchor,
                GbsLocateBaseStationAction<GbsAnchor>
            >();
            return builder;
        }
    }
}
