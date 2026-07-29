using Asv.Avalonia;
using Asv.Common;
using Asv.Drones.Api;
using Asv.Mavlink;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public sealed class GbsLocateBaseStationAction<TTarget>()
    : GbsActionBase<TTarget>("locate-base-station")
    where TTarget : class, IViewModel, IDeviceActionTarget<GbsClientDevice>
{
    public const MaterialIconKind ActionIcon = MaterialIconKind.Crosshairs;

    protected override IMenuItem? TryCreateAction(
        TTarget widget,
        CompositeDisposable contextDispose
    )
    {
        var gbs = TryGetGbsClient(widget);
        var map = widget.FindParentOfType<IFlightModePage>()?.Map;
        if (gbs is null || map is null)
        {
            return null;
        }

        var item = CreateMenuItem(RS.GbsLocateBaseStationAction_Header);
        item.StaysOpenOnClick = true;
        item.Icon = ActionIcon;
        item.Description = RS.GbsLocateBaseStationAction_Description;
        item.Order = 90;
        item.Command = CreateCommand(
                item,
                _ =>
                {
                    map.CenterMap.Value = gbs.Position.CurrentValue;
                    return ValueTask.CompletedTask;
                }
            )
            .DisposeItWith(contextDispose);

        return item;
    }
}
