using Asv.Avalonia;
using Asv.Avalonia.GeoMap;
using Asv.Avalonia.IO;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Asv.Modeling;
using Material.Icons;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class GbsAnchor : MapAnchor<GbsAnchor>
{
    public const string UavAnchorIdBase = "gbs";

    public GbsAnchor()
        : base(DesignTime.Id.TypeId, DesignTime.ExtensionService)
    {
        DesignTime.ThrowIfNotDesignMode();
    }

    public GbsAnchor(GbsClientDevice gbs, IDeviceManager mng, IExtensionService ext)
        : base(
            UavAnchorIdBase,
            new NavArgs(new KeyValuePair<string, string?>("deviceId", gbs.Id.AsString())),
            ext
        )
    {
        var pos =
            gbs.GetMicroservice<IAsvGbsExClient>()
            ?? throw new InvalidOperationException($"{nameof(IAsvGbsExClient)} not found");

        DeviceId = gbs.Id;
        IsReadOnly = true;
        IsVisible = true;
        Icon = mng.GetIcon(DeviceId) ?? MaterialIconKind.Memory;
        IconColor = mng.GetDeviceColor(DeviceId);
        CenterX = DeviceIconMixin.GetIconCenterX(DeviceId);
        CenterY = DeviceIconMixin.GetIconCenterY(DeviceId);
        UseMapRotation = false;
        gbs.Name.DistinctUntilChanged()
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .ObserveOnUIThreadDispatcher()
            .Subscribe(header => Header = header)
            .DisposeItWith(Disposable);
        pos.Position.DistinctUntilChanged()
            .Where(p =>
                !double.IsNaN(p.Latitude) && !double.IsNaN(p.Longitude) && !double.IsNaN(p.Altitude)
            )
            .ThrottleLast(TimeSpan.FromMilliseconds(200))
            .ObserveOnUIThreadDispatcher()
            .Subscribe(p => Location = p)
            .DisposeItWith(Disposable);
    }

    public DeviceId DeviceId { get; }
}
