using System.ComponentModel.Composition;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Asv.Cfg;
using Asv.Common;
using Asv.Mavlink;
using Asv.Mavlink.Client;
using Asv.Mavlink.Server;
using Asv.Mavlink.V2.Common;
using Newtonsoft.Json;
using NLog;

namespace Asv.Drones.Gbs.Core.Vehicles;

public class VehicleServiceConfig
{
    public int DeviceTimeoutMs { get; set; } = 10000;
}




    [Export(typeof(IVehicleServices))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class VehicleService : DisposableOnceWithCancel, IVehicleServices
    {
        private readonly IGbsMavlinkService _mavlink;
        private readonly IPacketSequenceCalculator _sequenceCalculator;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ReaderWriterLockSlim _deviceListLock = new();
        private readonly Subject<IMavlinkDeviceInfo> _foundDeviceSubject = new();
        private readonly Subject<IMavlinkDeviceInfo> _lostDeviceSubject = new();
        private readonly List<MavlinkDevice> _info = new();
        private readonly VehicleServiceConfig _config;
        private readonly TimeSpan _linkTimeout;


        [ImportingConstructor]
        public VehicleService(IGbsMavlinkService mavlink, IPacketSequenceCalculator sequenceCalculator, IConfiguration cfgSvc)
        {
            _mavlink = mavlink ?? throw new ArgumentNullException(nameof(mavlink));
            _config = cfgSvc.Get<VehicleServiceConfig>();
            _linkTimeout = TimeSpan.FromMilliseconds(_config.DeviceTimeoutMs);
            _sequenceCalculator = sequenceCalculator;
            Disposable.AddAction(() =>
            {
                _foundDeviceSubject.OnCompleted();
                _foundDeviceSubject.Dispose();
                _lostDeviceSubject.OnCompleted();
                _lostDeviceSubject.Dispose();
            });
            _mavlink.Router.Where(_ => _.MessageId == HeartbeatPacket.PacketMessageId).Cast<HeartbeatPacket>().Where(FilterVehicle).Subscribe(DeviceFounder, DisposeCancel);
            Observable.Timer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)).Subscribe(_ => RemoveOldDevice(), DisposeCancel);
        }

        private bool FilterVehicle(HeartbeatPacket packet)
        {
            return packet.Payload.Autopilot is MavAutopilot.MavAutopilotArdupilotmega or MavAutopilot.MavAutopilotPx4;
        }

        private void DeviceFounder(HeartbeatPacket packet)
        {
            MavlinkDevice newItem = null;
            _deviceListLock.EnterUpgradeableReadLock();
            var founded = _info.Find(_ => _.Packet.SystemId == packet.SystemId && _.Packet.ComponenId == packet.ComponenId);
            if (founded != null)
            {
                founded.Touch();
            }
            else
            {
                _deviceListLock.EnterWriteLock();
                newItem = new MavlinkDevice(packet, _mavlink.Server, _sequenceCalculator);
                _info.Add(newItem);
                _deviceListLock.ExitWriteLock();
                Logger.Info($"Found new device {JsonConvert.SerializeObject(newItem.GetInfo())}");
            }
            _deviceListLock.ExitUpgradeableReadLock();

            if (newItem != null) _foundDeviceSubject.OnNext(newItem.GetInfo());
        }

        private void RemoveOldDevice()
        {
            _deviceListLock.EnterUpgradeableReadLock();
            var now = DateTime.Now;
            var deviceToRemove = _info.Where(_ => (now - _.GetLastHit()) > _linkTimeout).ToArray();
            if (deviceToRemove.Length != 0)
            {
                _deviceListLock.EnterWriteLock();
                foreach (var device in deviceToRemove)
                {
                    _info.Remove(device);
                    Logger.Info($"Delete device {JsonConvert.SerializeObject(device.GetInfo())}");
                    device?.Dispose();
                }
                _deviceListLock.ExitWriteLock();
            }
            _deviceListLock.ExitUpgradeableReadLock();
            foreach (var dev in deviceToRemove)
            {
                _lostDeviceSubject.OnNext(dev.GetInfo());
            }

        }

        protected override void InternalDisposeOnce()
        {
            _deviceListLock.EnterReadLock();
            foreach (var deviceInfo in _info)
            {
                deviceInfo?.Dispose();
            }
            _deviceListLock.ExitReadLock();
            base.InternalDisposeOnce();
        }

        public IObservable<IMavlinkDeviceInfo> OnFoundNewVehicles => _foundDeviceSubject;
        public IObservable<IMavlinkDeviceInfo> OnLostVehicles => _lostDeviceSubject;

        public IMavlinkDeviceInfo[] Vehicles
        {
            get
            {
                _deviceListLock.EnterReadLock();
                var items = _info.Select(_ => _.GetInfo()).ToArray();
                _deviceListLock.ExitReadLock();
                return items;
            }
        }

        public async Task SendRtcmData(byte[] data, int length, CancellationToken cancel)
        {
            _deviceListLock.EnterReadLock();
            var vehicles = _info.ToArray();
            _deviceListLock.ExitReadLock();
            foreach (var vehicle in vehicles)
            {
                await vehicle.SendRtcmData(data, length, cancel).ConfigureAwait(false);
            }
        }

        class MavlinkDevice : IDisposable
        {
            private long _lastHit;
            private readonly DgpsClient _dgpsClient;
            public HeartbeatPacket Packet { get; }

            public MavlinkDevice(HeartbeatPacket packet, IMavlinkServer server, IPacketSequenceCalculator sequenceCalculator)
            {
                Packet = packet;
                Touch();
                IMavlinkDeviceInfo info = new GroundControlStation.MavlinkDeviceInfo(Packet);
                _dgpsClient = new DgpsClient(server.MavlinkV2Connection,
                    new MavlinkClientIdentity
                    {
                        ComponentId = server.Identity.ComponentId,
                        SystemId = server.Identity.SystemId,
                        TargetComponentId = (byte)info.ComponentId,
                        TargetSystemId = (byte)info.SystemId
                    }, sequenceCalculator,Scheduler.Default);

            }

            public DateTime GetLastHit()
            {
                var lastHit = Interlocked.CompareExchange(ref _lastHit, 0, 0);
                return DateTime.FromBinary(lastHit);
            }

            public void Touch()
            {
                Interlocked.Exchange(ref _lastHit, DateTime.Now.ToBinary());
            }

            public IMavlinkDeviceInfo GetInfo()
            {
                return new GroundControlStation.MavlinkDeviceInfo(Packet);
            }

            public Task SendRtcmData(byte[] data, int length, CancellationToken cancel)
            {
                return _dgpsClient.SendRtcmData(data, length, cancel);
            }

            public void Dispose()
            {
                _dgpsClient?.Dispose();
            }
        }
    }