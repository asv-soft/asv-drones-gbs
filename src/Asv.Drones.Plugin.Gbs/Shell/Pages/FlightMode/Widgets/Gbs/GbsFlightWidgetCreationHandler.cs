using Asv.Drones.Api;
using Asv.IO;
using Asv.Mavlink;
using Microsoft.Extensions.DependencyInjection;

namespace Asv.Drones.Plugin.Gbs;

public class GbsFlightWidgetCreationHandler(IServiceProvider services)
    : IClientDeviceWidgetCreationHandler
{
    public Type DeviceType => typeof(GbsClientDevice);

    public IFlightWidget? Create(in IClientDevice device)
    {
        if (device.GetMicroservice<IAsvGbsExClient>() is null)
        {
            return null;
        }

        if (device is not GbsClientDevice mavlinkDevice)
        {
            return null;
        }

        return ActivatorUtilities.CreateInstance<GbsFlightWidgetViewModel>(services, mavlinkDevice);
    }
}
