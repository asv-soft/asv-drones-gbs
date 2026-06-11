using Asv.Avalonia;
using Asv.Avalonia.IO;
using Asv.Common;
using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Asv.Modeling;
using Avalonia.Controls;
using ObservableCollections;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public class GbsFlightWidgetViewModel
    : MavlinkDeviceFlightWidgetViewModelBase<GbsClientDevice, IGbsFlightWidget>,
        IGbsFlightWidget
{
    public const string WidgetId = "gbs";

    public GbsFlightWidgetViewModel(
        GbsClientDevice device,
        IDeviceManager deviceManager,
        IExtensionService ext
    )
        : base(
            new NavId(WidgetId, DevicePageViewModelMixin.CreateOpenPageArgs(device.Id)),
            device,
            deviceManager,
            ext
        ) { }
}
