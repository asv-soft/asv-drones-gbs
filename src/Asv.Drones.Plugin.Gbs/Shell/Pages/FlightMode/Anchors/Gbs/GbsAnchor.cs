using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class GbsAnchor : DeviceAnchor<GbsAnchor>, IDeviceActionTarget<GbsClientDevice>
{
    public const string UavAnchorIdBase = "gbs";

    public GbsAnchor(GbsClientDevice gbs, IDeviceManager mng, IExtensionService ext)
        : base(UavAnchorIdBase, [], mng, gbs, ext)
    {
        Device = gbs;
        var gbsClient = gbs.GetRequiredMicroservice<IAsvGbsExClient>();

        IsReadOnly = true;
        IsVisible = true;
        UseMapRotation = false;
        gbsClient
            .Position.DistinctUntilChanged()
            .ObserveOnUIThreadDispatcher()
            .Subscribe(x => Location = x)
            .DisposeItWith(Disposable);
    }

    public new GbsClientDevice Device { get; }
}
