using System.ComponentModel.Composition;
using System.IO.Ports;
using System.Reactive.Linq;
using Asv.Cfg;
using Asv.Common;
using Asv.Gnss;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.V2.AsvGbs;
using Asv.Mavlink.V2.Common;
using NLog;

namespace Asv.Drones.Gbs;

/// <summary>
/// Represents the configuration settings for the UbloxRtkModule.
/// </summary>
public class UbloxRtkModuleConfig
{
#if DEBUG
    /// <summary>
    /// Gets or sets the value indicating whether the property is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> if the property is enabled; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property is used to determine if a certain functionality or feature is enabled or disabled.
    /// The default value is <c>false</c>.
    /// </remarks>
    public bool IsEnabled { get; set; } = false;
#else
    public bool IsEnabled { get; set; } = true;
#endif
    /// <summary>
    /// Gets or sets the connection string for the serial port.
    /// </summary>
    /// <value>
    /// The connection string in the format: "serial:/dev/ttyACM0?br=115200".
    /// </value>
    public string ConnectionString { get; set; } = "serial:/dev/ttyACM0?br=115200";

    /// <summary>
    /// Gets or sets the rate at which the GBS status is updated in milliseconds.
    /// </summary>
    /// <value>
    /// The rate at which the GBS status is updated in milliseconds. The default value is 1000 milliseconds.
    /// </value>
    public int GbsStatusRateMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the rate at which the status is updated from the device, in milliseconds.
    /// </summary>
    /// <value>
    /// The rate at which the status is updated from the device, in milliseconds.
    /// </value>
    public int UpdateStatusFromDeviceRateMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the array of RTCMv3 message IDs to send.
    /// </summary>
    /// <remarks>
    /// The message IDs indicate the specific RTCMv3 messages that
    /// should be sent by the software. The array contains ushort values
    /// representing the message IDs.
    /// </remarks>
    /// <value>
    /// An array of ushort values representing RTCMv3 message IDs.
    /// </value>
    public ushort[] RtcmV3MessagesIdsToSend { get; set; } = {
        1005 , 1006 , 1074 , 1077 ,
        1084 , 1087 , 1094 , 1097 ,
        1124 , 1127 , 1230 , 4072
    };

    /// <summary>
    /// Gets or sets the rate at which messages are processed in hertz (Hz).
    /// </summary>
    /// <remarks>
    /// The MessageRateHz property determines how often messages are processed.
    /// It represents the number of messages that can be processed in one second.
    /// By default, the value is set to 1 Hz.
    /// </remarks>
    /// <value>
    /// The rate at which messages are processed in hertz.
    /// </value>
    public byte MessageRateHz { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timeout value in milliseconds for reconnecting.
    /// </summary>
    /// <value>
    /// The timeout value in milliseconds for reconnecting. The default value is 10,000 milliseconds.
    /// </value>
    public int ReconnectTimeoutMs { get; set; } = 10_000;
}

/// <summary>
/// Represents the UBlox RTK module.
/// </summary>
[Export(typeof(IModule))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class UbloxRtkModule: DisposableOnceWithCancel, IModule
{
    /// <summary>
    /// Represents an instance of the IGbsMavlinkService interface.
    /// </summary>
    private readonly IGbsMavlinkService _svc;

    /// <summary>
    /// Represents the configuration of the UbloxRtkModule.
    /// </summary>
    private readonly UbloxRtkModuleConfig _config;

    /// <summary>
    /// Logger instance for logging events and messages.
    /// </summary>
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Represents a UbxDevice object.
    /// </summary>
    private UbxDevice? _device;

    /// <summary>
    /// Represents the current busy status.
    /// </summary>
    private int _busy;

    /// <summary>
    /// This private variable indicates whether the initialization process has been completed.
    /// </summary>
    private bool _isInit;

    /// <summary>
    /// Represents the status of a ongoing update operation.
    /// </summary>
    /// <remarks>
    /// This variable is used to keep track of the progress of an update operation.
    /// It stores an integer value that represents the current status of the ongoing update.
    /// </remarks>
    private int _updateStatusInProgress;

    /// <summary>
    /// Represents a read-only Reactive Variable storing UbxNavSvin data.
    /// </summary>
    private readonly RxValue<UbxNavSvin> _svIn;

    /// <summary>
    /// Private variable representing the flag indicating whether to send RTCM data.
    /// </summary>
    private int _sendRtcmFlag;

    /// <summary>
    /// Represents the rate at which bytes are received.
    /// </summary>
    private readonly IncrementalRateCounter _rxByteRate = new(3);

    /// <summary>
    /// Represents the number of received bytes.
    /// </summary>
    private long _rxBytes;

    /// <summary>
    /// Indicates whether the system is currently sending RTCM data.
    /// </summary>
    private bool _areRtcmSending;

    /// <summary>
    /// Represents a UbloxRtkModule that handles communication with a Ublox RTK module.
    /// </summary>
    [ImportingConstructor]
    public UbloxRtkModule(IGbsMavlinkService svc,IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        _config = configuration.Get<UbloxRtkModuleConfig>();
        // if disabled => do nothing
        if (_config.IsEnabled == false)
        {
            Logger.Warn("UBlox RTK module is disbaled and will be ignored");
            return;
        }

        _svc.Server.Gbs.StartAutoMode = StartAutoMode;
        _svc.Server.Gbs.StartFixedMode = StartFixedMode;
        _svc.Server.Gbs.StartIdleMode = StartIdleMode;
        
        _svIn = new RxValue<UbxNavSvin>().DisposeItWith(Disposable);
        
        Observable.Timer(TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs), TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs))
            .Subscribe(UpdateStatus)
            .DisposeItWith(Disposable);
    }

    /// <summary>
    /// Sends a raw RTCMv3 message.
    /// </summary>
    /// <param name="msg">The RTCMv3 raw message to be sent.</param>
    private void SendRtcm(RtcmV3RawMessage msg)
    {
        if (_isInit == false) return;
       
        if (Interlocked.CompareExchange(ref _sendRtcmFlag, 1, 0) != 0)
        {
            return;
        }
        try
        {
            Interlocked.Add(ref _rxBytes, msg.RawData.Length);
            var rate = _rxByteRate.Calculate(_rxBytes);
            _svc.Server.Gbs.DgpsRate.OnNext((ushort)rate);
            _svc.Server.Gbs.SendRtcmData(msg.RawData, msg.RawData.Length, CancellationToken.None).Wait();
        }
        finally
        {
            Interlocked.Exchange(ref _sendRtcmFlag, 0);
        }
    }

    /// <summary>
    /// Handles the process of sending RTCM MSM4 messages asynchronously.
    /// </summary>
    /// <param name="cancel">Cancellation token to allow for cancellation of the operation.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task RtcmMSM4On(CancellationToken cancel)
    {
        if (_areRtcmSending) return;
        await _device.SetupRtcmMSM4Rate(_config.MessageRateHz, cancel).ConfigureAwait(false);
        _areRtcmSending = true;
    }

    /// <summary>
    /// Turns off the Real-Time Kinematic (RTK) Compact Measurement Message (CMM) Stream.
    /// </summary>
    /// <param name="cancel">The cancellation token to cancel the async operation.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task RtcmMSMOff(CancellationToken cancel)
    {
        if (!_areRtcmSending) return;
        await _device.SetupRtcmMSM4Rate(0, cancel).ConfigureAwait(false);
        _areRtcmSending = false;
    }

    /// <summary>
    /// Updates the status asynchronously.
    /// </summary>
    /// <param name="l">The long value.</param>
    private async void UpdateStatus(long l)
    {
        if (Interlocked.CompareExchange(ref _updateStatusInProgress,1,0) !=0) return;
        try
        {
            if (_isInit == false) return;

            var cfgTMode3 = await _device.GetCfgTMode3(DisposeCancel);
            switch (cfgTMode3.Mode)
            {
                case TMode3Enum.Disabled:
                    _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
                    await RtcmMSMOff(DisposeCancel).ConfigureAwait(false);
                    break;
                case TMode3Enum.SurveyIn:
                    var state = _svIn.Value.Active
                        ? AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress
                        : AsvGbsCustomMode.AsvGbsCustomModeAuto;
                    _svc.Server.Gbs.CustomMode.OnNext(state);
                    _svc.Server.Gbs.Position.OnNext(_svIn.Value.Location ?? GeoPoint.Zero);
                    if (state == AsvGbsCustomMode.AsvGbsCustomModeAuto)
                    {
                        await RtcmMSM4On(DisposeCancel).ConfigureAwait(false);
                    }
                    else
                    {
                        await RtcmMSMOff(DisposeCancel).ConfigureAwait(false);
                    }

                    break;
                case TMode3Enum.FixedMode:
                    _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeFixed);
                    _svc.Server.Gbs.Position.OnNext(cfgTMode3.Location ?? GeoPoint.Zero);
                    await RtcmMSM4On(DisposeCancel).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _svc.Server.Gbs.AccuracyMeter.OnNext(_svIn.Value.Accuracy);
            _svc.Server.Gbs.ObservationSec.OnNext((ushort)_svIn.Value.Observations);

            var navSat = await _device.GetNavSat(DisposeCancel);
            _svc.Server.Gbs.AllSatellites.OnNext(navSat.NumSvs);
            byte gps = 0;
            byte sbas = 0;
            byte galileo = 0;
            byte beidou = 0;
            byte imes = 0;
            byte qzss = 0;
            byte glo = 0;
            foreach (var satItem in navSat.Items)
            {
                switch (satItem.GnssType)
                {
                    case UbxNavSatGnssId.GPS:
                        ++gps;
                        break;
                    case UbxNavSatGnssId.SBAS:
                        ++sbas;
                        break;
                    case UbxNavSatGnssId.Galileo:
                        ++galileo;
                        break;
                    case UbxNavSatGnssId.BeiDou:
                        ++beidou;
                        break;
                    case UbxNavSatGnssId.IMES:
                        ++imes;
                        break;
                    case UbxNavSatGnssId.QZSS:
                        ++qzss;
                        break;
                    case UbxNavSatGnssId.GLONASS:
                        ++glo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _svc.Server.Gbs.GpsSatellites.OnNext(gps);
            _svc.Server.Gbs.SbasSatellites.OnNext(sbas);
            _svc.Server.Gbs.GalSatellites.OnNext(galileo);
            _svc.Server.Gbs.BeidouSatellites.OnNext(beidou);
            _svc.Server.Gbs.ImesSatellites.OnNext(imes);
            _svc.Server.Gbs.QzssSatellites.OnNext(qzss);
            _svc.Server.Gbs.GlonassSatellites.OnNext(glo);
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            Interlocked.Exchange(ref _updateStatusInProgress, 0);
        }
    }

    

    #region Init

    /// <summary>
    /// Initializes the method.
    /// </summary>
    public void Init()
    {
        // if disabled => do nothing
        if (_config.IsEnabled == false) return;
        Observable.Timer(TimeSpan.FromMilliseconds(1000)).Subscribe(_ => InitUbxDevice(),DisposeCancel);
    }


    /// <summary>
    /// Returns a default instance of <see cref="IGnssConnection"/> using the specified <paramref name="port"/>.
    /// </summary>
    /// <param name="port">The optional port to use for the connection.</param>
    /// <returns>A default instance of <see cref="IGnssConnection"/>.</returns>
    private IGnssConnection GetDefaultUbxConnection(IPort? port)
    {
        return new GnssConnection(port, new Nmea0183Parser().RegisterDefaultMessages(),
            new RtcmV3Parser().RegisterDefaultMessages(), new UbxBinaryParser().RegisterDefaultMessages());
    }

    /// <summary>
    /// Configures the baud rate and creates a UbxDevice.
    /// </summary>
    /// <param name="currentConfig">The current serial port configuration.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the UbxDevice.</returns>
    private async Task<UbxDevice> ConfigureBaudRateAndCreateDevice(SerialPortConfig currentConfig)
    {
        var availableBr = new[] { currentConfig.BoundRate, 9600, 38400, 57600, 115200, 230400, 460800 }.Distinct().ToArray();
        var requiredBoundRate = currentConfig.BoundRate;
        Exception? lastEx = null;
        foreach (var br in availableBr)
        {
            UbxDevice? device = null;
            CustomSerialPort? port = null;
            try
            {
                currentConfig.BoundRate = br;
                port = new CustomSerialPort(currentConfig);
                port.Enable();
                device = new UbxDevice(GetDefaultUbxConnection(port), UbxDeviceConfig.Default).DisposeItWith(Disposable);
                var cfgPort = (UbxCfgPrtConfigUart)(await device.GetCfgPort(1, DisposeCancel).ConfigureAwait(false)).Config;
                _svc.Server.StatusText.Info($"GNSS device BoundRate: {cfgPort.BoundRate}");
                if (cfgPort.BoundRate == requiredBoundRate) return device;
                
                _svc.Server.StatusText.Info($"Change BoundRate {cfgPort.BoundRate} => {requiredBoundRate}");
                await device
                    .SetCfgPort(
                        new UbxCfgPrt
                        {
                            Config = new UbxCfgPrtConfigUart { PortId = 1, BoundRate = requiredBoundRate }
                        }, DisposeCancel).ConfigureAwait(false);
                device.Dispose();
                port.Disable();
                port.Dispose();
                currentConfig.BoundRate = requiredBoundRate;
                port = new CustomSerialPort(currentConfig);
                port.Enable();
                device = new UbxDevice(GetDefaultUbxConnection(port), UbxDeviceConfig.Default)
                    .DisposeItWith(Disposable);
                
                cfgPort = (UbxCfgPrtConfigUart)(await device.GetCfgPort(1, DisposeCancel).ConfigureAwait(false)).Config;
                _svc.Server.StatusText.Info($"GNSS device BoundRate: {cfgPort.BoundRate}");
                if (cfgPort.BoundRate == requiredBoundRate) return device;
            }
            catch (Exception e)
            {
                device?.Dispose();
                port?.Disable();
                port?.Dispose();
                lastEx = e;
            }
        }

        throw lastEx!;
    }

    /// <summary>
    /// Initializes the UBX GNSS device.
    /// </summary>
    private async void InitUbxDevice()
    {
        try
        {
            if (_device == null)
            {
                // start to init device
                _svc.Server.StatusText.Info("Connecting to GNSS device...");
                var port = PortFactory.Create(_config.ConnectionString, true);
                if (port.PortType == PortType.Serial)
                {
                    port.Disable();
                    port.Dispose();
                    var uri = new Uri(_config.ConnectionString);
                    SerialPortConfig.TryParseFromUri(uri, out var portConf);
                    _device = await ConfigureBaudRateAndCreateDevice(portConf).ConfigureAwait(false);
                }
                else
                {
                    _device =
                        new UbxDevice(GetDefaultUbxConnection(port), UbxDeviceConfig.Default)
                            .DisposeItWith(Disposable);
                }
                var rtcmV3Filter = new HashSet<ushort>(_config.RtcmV3MessagesIdsToSend);
                _device?.Connection.Filter<UbxNavSvin>().Subscribe(_svIn).DisposeItWith(Disposable);
                _device?.Connection.GetRtcmV3RawMessages().Where(_=>rtcmV3Filter.Contains(_.MessageId)).Subscribe(SendRtcm)
                    .DisposeItWith(Disposable);
            }
            
            _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeLoading);
            var ver = await _device.GetMonVer();
            _svc.Server.StatusText.Debug($"Found GNSS HW:{ver.Hardware.Trim('\0')}");
            _svc.Server.StatusText.Debug($"GNSS SW:{ver.Software.Trim('\0')}");
            var ext = ver.Extensions.Select(_ => _.Trim('\0')).Distinct().ToArray();
            _svc.Server.StatusText.Debug($"GNSS EXT:{string.Join(",", ext)}");
            await _device.SetStationaryMode(false, _config.MessageRateHz);
            await _device.TurnOffNmea(CancellationToken.None);
            // surveyin msg - for feedback
            await _device.SetMessageRate<UbxNavSvin>(_config.MessageRateHz);
            // pvt msg - for feedback
            await _device.SetMessageRate<UbxNavPvt>(_config.MessageRateHz);
            // 1005 - 5s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RTCM3, 0x05, 5);
            
            await _device.SetupRtcmMSM4Rate(_config.MessageRateHz,DisposeCancel);
            _areRtcmSending = true;
            
            // 1230 - 5s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RTCM3, 0xE6, 5);
            
            // NAV-VELNED - 1s
            
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.NAV, 0x12, _config.MessageRateHz);
            
            // rxm-raw/rawx - 1s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x15, _config.MessageRateHz);
            //await SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x10, 1, cancel);
            
            // rxm-sfrb/sfrb - 2s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x13, 2, default);
            //await SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x11, 2, cancel);
            
            // mon-hw - 2s
            await _device.SetMessageRate((byte)UbxHelper.ClassIDs.MON, 0x09, 2, default);
            _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeIdle);
            
            _isInit = true;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("Error to init GNSS");
            // _svc.Server.StatusText.Debug(e.Message);
            _svc.Server.StatusText.Debug($"Reconnect after {TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs).TotalSeconds:F0} sec...");
            _svc.Server.Gbs.CustomMode.OnNext(AsvGbsCustomMode.AsvGbsCustomModeError);
            Observable.Timer(TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs)).Subscribe(_ => InitUbxDevice(),DisposeCancel);
        }
    }

    #endregion

    /// <summary>
    /// Start the GNSS AUTO mode with the given duration and accuracy.
    /// </summary>
    /// <param name="duration">The duration in seconds for which the GNSS AUTO mode should be active.</param>
    /// <param name="accuracy">The desired accuracy for the GNSS AUTO mode.</param>
    /// <param name="cancel">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. It returns MavResult.MavResultAccepted if the GNSS AUTO mode is started successfully, MavResult.MavResultTemporarilyRejected if the initialization and begin call check fails, or MavResult.MavResultFailed if there is an error during the operation.</returns>
    public async Task<MavResult> StartAutoMode(float duration, float accuracy, CancellationToken cancel)
    {
        if (CheckInitAndBeginCall() == false) return MavResult.MavResultTemporarilyRejected;
        try
        {
            var mode = await _device.GetCfgTMode3(cancel);
            if (mode.Mode == TMode3Enum.SurveyIn)
            {
                await _device.Push(new UbxCfgTMode3 { Mode = TMode3Enum.Disabled, IsGivenInLLA = false }, cancel)
                    .ConfigureAwait(false);
            }
            _svc.Server.StatusText.Info($"Set GNSS AUTO mode (dur:{duration:F0},acc:{accuracy:F0})");
            await _device.SetSurveyInMode((uint)duration, accuracy, cancel);

            if (mode.Mode == TMode3Enum.FixedMode)
            {
                await _device.RebootReceiver(cancel).ConfigureAwait(false);
            }
            
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("GNSS AUTO mode error");
            _svc.Server.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }

   

    #region Checks

    /// <summary>
    /// Ends the call and updates the '_busy' flag to indicate that the call has ended.
    /// </summary>
    private void EndCall()
    {
        Interlocked.Exchange(ref _busy, 0);
    }

    /// <summary>
    /// Checks if the initialization is complete and begins the method call.
    /// </summary>
    /// <returns>Returns true if the initialization is complete and the method call can proceed.
    /// Returns false if the initialization is not complete or if there is an ongoing method call.</returns>
    private bool CheckInitAndBeginCall()
    {
        // this is for reject duplicate requests
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            _svc.Server.StatusText.Warning("Temporarily rejected: now is busy");
            return false;
        }

        if (_isInit == false)
        {
            _svc.Server.StatusText.Warning("Temporarily rejected: now is busy");
            return false;
        }

        return true;
    }

    #endregion

    /// <summary>
    /// Start Fixed mode with specified GeoPoint and accuracy
    /// </summary>
    /// <param name="geoPoint">The GeoPoint to set</param>
    /// <param name="accuracy">The accuracy value</param>
    /// <param name="cancel">The CancellationToken for cancellation</param>
    /// <returns>The Task that represents the asynchronous operation with MavResult value</returns>
    public async Task<MavResult> StartFixedMode(GeoPoint geoPoint,float accuracy, CancellationToken cancel)
    {
        if (CheckInitAndBeginCall() == false) return MavResult.MavResultTemporarilyRejected;

        try
        {
            _svc.Server.StatusText.Info($"Set GNSS FIXED mode ({geoPoint})");
            await _device.SetFixedBaseMode(geoPoint,accuracy,cancel).ConfigureAwait(false);
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("GNSS FIXED mode error");
            _svc.Server.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }

    /// <summary>
    /// Starts the idle mode of the receiver.
    /// </summary>
    /// <param name="cancel">The cancellation token to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        if (CheckInitAndBeginCall() == false) return MavResult.MavResultTemporarilyRejected;
        try
        {
            await _device.Push(new UbxCfgTMode3 { Mode = TMode3Enum.Disabled, IsGivenInLLA = false }, cancel)
                .ConfigureAwait(false);
            await _device.RebootReceiver(cancel).ConfigureAwait(false);
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.Server.StatusText.Error("GNSS IDLE mode error");
            _svc.Server.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }
}