using Asv.Mavlink;

namespace Asv.Drones.Gbs.Core.Vehicles;

public interface IVehicleServices : IDisposable
{
    IObservable<byte> OnVehicleCountChanged { get; }
    IObservable<IMavlinkDeviceInfo> OnFoundNewVehicles { get; }
    IObservable<IMavlinkDeviceInfo> OnLostVehicles { get; }
    IMavlinkDeviceInfo[] Vehicles { get; }
    Task SendRtcmData(byte[] data, int length, CancellationToken cancel);

}

