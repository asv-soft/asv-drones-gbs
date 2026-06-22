using System.Collections.Specialized;
using System.Diagnostics.Metrics;
using Asv.Common;
using Asv.Gnss;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.AsvGbs;
using Asv.Mavlink.Common;
using DotNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.Drones.Gbs;

public static class UBloxRtkHandlerMixin
{
    public static IHostApplicationBuilder AddRtkHandler(this IHostApplicationBuilder builder)
    {
        builder
            .Services.AddHostedService<UBloxRtkHandler>()
            .AddOptions<RtkDeviceOptions>()
            .Bind(builder.Configuration.GetSection(RtkDeviceOptions.Section));
        return builder;
    }
}

static class RtkFixedModeValidator
{
    public const float MinAccuracyMeters = 0.001f;
    public const float MaxAccuracyMeters = 100.0f;

    public static bool TryValidate(GeoPoint position, float accuracy, out string error)
    {
        if (IsFinite(position.Latitude) == false)
        {
            error = "Fixed Mode latitude is not finite.";
            return false;
        }

        if (IsFinite(position.Longitude) == false)
        {
            error = "Fixed Mode longitude is not finite.";
            return false;
        }

        if (IsFinite(position.Altitude) == false)
        {
            error = "Fixed Mode altitude is not finite.";
            return false;
        }

        if (position.Latitude is < -90.0 or > 90.0)
        {
            error = $"Fixed Mode latitude is out of range: {position.Latitude}.";
            return false;
        }

        if (position.Longitude is < -180.0 or > 180.0)
        {
            error = $"Fixed Mode longitude is out of range: {position.Longitude}.";
            return false;
        }

        if (
            Math.Abs(position.Latitude) < double.Epsilon
            && Math.Abs(position.Longitude) < double.Epsilon
        )
        {
            error = "Fixed Mode coordinates are not configured: latitude and longitude are zero.";
            return false;
        }

        if (IsFinite(accuracy) == false)
        {
            error = "Fixed Mode accuracy is not finite.";
            return false;
        }

        if (accuracy is < MinAccuracyMeters or > MaxAccuracyMeters)
        {
            error =
                $"Fixed Mode accuracy must be between {MinAccuracyMeters} and {MaxAccuracyMeters} meters.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsFinite(double value) =>
        double.IsNaN(value) == false && double.IsInfinity(value) == false;
}

public class UBloxRtkHandler : AsyncDisposableWithCancel, IHostedService
{
    private readonly ILogger<UBloxRtkHandler> _logger;
    private readonly IMavlinkService _svc;

    /// <summary>
    /// Represents the configuration of the UbloxRtkModule.
    /// </summary>
    private readonly RtkDeviceOptions _config;

    private readonly IDeviceConnectionsService _connections;

    /// <summary>
    /// Represents the current busy status.
    /// </summary>
    private int _busy;

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
    private readonly ReactiveProperty<UbxNavSvin> _svIn;

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
    private long _lastRtcmMessageTickMs;
    private const long RtcmOutputStaleTimeoutMs = 10_000;
    private static readonly TimeSpan RtcmSendTimeout = TimeSpan.FromSeconds(2);

    private UbxRtkDevice? _device;
    private readonly IProtocolRouter _router;
    private IDisposable? _initUbxSub = null;
    private IDisposable? _connToDevSub = null;
    private readonly ReactiveProperty<UbxNavPvt> _pvt;
    private GeoPoint _lastPositon;
    private readonly IReadOnlyObservableList<IClientDevice> _allDevices;
    private IDisposable? _navSvInSubscription = null;
    private IDisposable? _navPvtSubscription = null;
    private IDisposable? _rtcmV3Subscription = null;

    public UBloxRtkHandler(
        IMavlinkService mavlink,
        IOptions<RtkDeviceOptions> config,
        IDeviceConnectionsService connections,
        ILoggerFactory loggerFactory,
        IMeterFactory meterFactory
    )
    {
        _logger = loggerFactory.CreateLogger<UBloxRtkHandler>();
        _svc = mavlink;
        _config = config.Value;
        _connections = connections;

        var factory = Protocol.Create(builder =>
        {
            builder.SetLog(loggerFactory);
            builder.SetMetrics(meterFactory);
            builder.Features.RegisterBroadcastAllFeature();
            builder.Features.RegisterEndpointIdTagFeature();
            builder.Protocols.RegisterNmeaProtocol();
            builder.Protocols.RegisterRtcmV3Protocol();
            builder.Protocols.RegisterUbxProtocol();
        });

        _router = factory.CreateRouter("UBX");
        _router.RegisterTo(DisposeCancel);

        var browser = DeviceExplorer.Create(
            _router,
            builder =>
            {
                builder.SetLog(loggerFactory);
                builder.Factories.RegisterGnssDevice();
            }
        );

        var dev = browser.InitializedDevices.FirstOrDefault(d =>
            d.Id.DeviceClass == GnssDeviceId.GnssDeviceClass
        );
        if (
            dev?.Microservices.FirstOrDefault(ms => ms is IUbxMicroserviceClient)
            is IUbxMicroserviceClient client
        )
        {
            _device = new UbxRtkDevice(client, dev, _config);
            DisposeCancel.Register(() => _device?.Dispose());
            InitUbxDevice(_device);
        }

        _allDevices = browser.InitializedDevices;
        browser.InitializedDevices.CollectionChanged += DevicesOnCollectionChanged;
        DisposeCancel.Register(() =>
            browser.InitializedDevices.CollectionChanged -= DevicesOnCollectionChanged
        );

        _svc.Gbs.StartIdleMode = StartIdleMode;
        if (_config.IsEnabledRtk)
        {
            _svc.Gbs.StartAutoMode = StartAutoMode;
            _svc.Gbs.StartFixedMode = StartFixedMode;
        }

        _svIn = new ReactiveProperty<UbxNavSvin>();
        _svIn.RegisterTo(DisposeCancel);
        _pvt = new ReactiveProperty<UbxNavPvt>();
        _pvt.RegisterTo(DisposeCancel);

        Observable
            .Timer(
                TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs),
                TimeSpan.FromMilliseconds(_config.UpdateStatusFromDeviceRateMs)
            )
            .Subscribe(UpdateStatus)
            .RegisterTo(DisposeCancel);
    }

    private void DevicesOnCollectionChanged(in NotifyCollectionChangedEventArgs<IClientDevice> e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItem.Id.DeviceClass == GnssDeviceId.GnssDeviceClass)
                {
                    if (
                        e.NewItem.Microservices.FirstOrDefault(ms => ms is IUbxMicroserviceClient)
                        is not IUbxMicroserviceClient client
                    )
                    {
                        return;
                    }

                    var device = new UbxRtkDevice(client, e.NewItem, _config);
                    DisposeCancel.Register(() => device.Dispose());
                    var oldDevice = _device;
                    Interlocked.Exchange(ref _device, device);
                    oldDevice?.Dispose();
                    _logger.ZLogInformation($"Added Ubx device: EndpointId='{_device?.Client.Id}'");
                    InitUbxDevice(device);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItem.Id.DeviceClass == GnssDeviceId.GnssDeviceClass)
                {
                    if (e.OldItem == _device?.Device)
                    {
                        var oldDeviceId = _device.Client.Id;
                        var oldDevice = _device;
                        Interlocked.Exchange(ref _device, null);
                        oldDevice?.Dispose();
                        _logger.ZLogInformation($"Removed Ubx device: EndpointId='{oldDeviceId}'");

                        var dev = _allDevices.FirstOrDefault(dev =>
                            dev.Id.DeviceClass == GnssDeviceId.GnssDeviceClass
                        );
                        if (
                            dev?.Microservices.FirstOrDefault(ms => ms is IUbxMicroserviceClient)
                            is IUbxMicroserviceClient client
                        )
                        {
                            var device = new UbxRtkDevice(client, dev, _config);
                            DisposeCancel.Register(() => device.Dispose());
                            _logger.ZLogInformation(
                                $"Changed Ubx device: From EndpointId='{oldDeviceId}' to EndpointId='{device.Client.Id}'"
                            );
                            Interlocked.Exchange(ref _device, device);
                            InitUbxDevice(device);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Sends a RTCMv3 message.
    /// </summary>
    /// <param name="msg">The RTCMv3 raw message to be sent.</param>
    private async Task SendRtcm(RtcmV3MessageBase msg)
    {
        if (_config.IsEnabledRtk == false)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _sendRtcmFlag, 1, 0) != 0)
        {
            return;
        }

        try
        {
            var size = msg.GetByteSize();
            Interlocked.Exchange(ref _lastRtcmMessageTickMs, Environment.TickCount64);
            Interlocked.Add(ref _rxBytes, size);
            var rate = _rxByteRate.Calculate(_rxBytes);
            _svc.Gbs.DgpsRate.Value = (ushort)rate;
            var buffer = new byte[size];
            var span = new Span<byte>(buffer);
            msg.Serialize(ref span);
            await _svc
                .Gbs.SendRtcmData(buffer, size, DisposeCancel)
                .WaitAsync(RtcmSendTimeout, DisposeCancel)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _logger.ZLogError($"Timeout sending Rtcm after {RtcmSendTimeout.TotalSeconds:F0} sec");
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"Error sending Rtcm: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _sendRtcmFlag, 0);
        }
    }

    /// <summary>
    /// Handles the process of sending RTCM messages asynchronously.
    /// </summary>
    /// <param name="cancel">Cancellation token to allow for cancellation of the operation.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task RtcmOn(CancellationToken cancel, bool force = false)
    {
        if (_config.IsEnabledRtk == false)
        {
            return;
        }

        if (_areRtcmSending && force == false && IsRtcmOutputStale() == false)
        {
            return;
        }

        if (_device == null || _device.IsInit == false)
        {
            return;
        }

        if (_areRtcmSending && IsRtcmOutputStale())
        {
            _logger.ZLogWarning($"RTCM output is stale. Reconfigure RTCM output.");
        }

        await _device.SetRtcmOutput(true, cancel).ConfigureAwait(false);
        _areRtcmSending = true;
    }

    private bool IsRtcmOutputStale()
    {
        var lastRtcmMessageTickMs = Interlocked.Read(ref _lastRtcmMessageTickMs);
        return lastRtcmMessageTickMs == 0
            || Environment.TickCount64 - lastRtcmMessageTickMs > RtcmOutputStaleTimeoutMs;
    }

    /// <summary>
    /// Turns off the Real-Time Kinematic (RTK) Compact Measurement Message (CMM) Stream.
    /// </summary>
    /// <param name="cancel">The cancellation token to cancel the async operation.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task RtcmOff(CancellationToken cancel, bool force = false)
    {
        if (!_areRtcmSending && force == false)
        {
            return;
        }

        if (_device == null || _device.IsInit == false)
        {
            return;
        }

        await _device.SetRtcmOutput(false, cancel).ConfigureAwait(false);
        Interlocked.Exchange(ref _lastRtcmMessageTickMs, 0);
        _areRtcmSending = false;
    }

    /// <summary>
    /// Updates the status asynchronously.
    /// </summary>
    /// <param name="l">The long value.</param>
    private async void UpdateStatus(Unit l)
    {
        if (Interlocked.CompareExchange(ref _updateStatusInProgress, 1, 0) != 0)
        {
            return;
        }

        try
        {
            if (_device == null || _device.IsInit == false)
            {
                return;
            }

            var position = GeoPoint.Zero;
            if (_pvt.Value != null)
            {
                position = new GeoPoint(
                    _pvt.Value.Latitude,
                    _pvt.Value.Longitude,
                    _pvt.Value.AltMsl
                );
                var fix = (int)_pvt.Value.FixType;
                if (fix > (int)UbxGnssFixType.DeadReckoningOnly)
                {
                    _lastPositon = position;
                }
                else
                {
                    position = _lastPositon;
                }
            }

            var cfgTMode3 = await _device?.Client.GetCfgTMode3(DisposeCancel)!;
            if (cfgTMode3 != null)
            {
                var svIn = _svIn.Value;
                var accuracy = svIn?.Accuracy ?? 0;
                var observations = svIn?.Observations ?? 0;
                switch (cfgTMode3.Mode)
                {
                    case TMode3Enum.Disabled:
                        _svc.Gbs.CustomMode.Value = AsvGbsCustomMode.AsvGbsCustomModeIdle;
                        _svc.Gbs.Position.Value = position;
                        await RtcmOff(DisposeCancel).ConfigureAwait(false);
                        break;
                    case TMode3Enum.SurveyIn:
                        var state = _svIn.Value.Active
                            ? AsvGbsCustomMode.AsvGbsCustomModeAutoInProgress
                            : AsvGbsCustomMode.AsvGbsCustomModeAuto;
                        _svc.Gbs.CustomMode.Value = state;
                        if (state == AsvGbsCustomMode.AsvGbsCustomModeAuto)
                        {
                            _svc.Gbs.Position.Value = _svIn.Value.Location ?? position;
                            await RtcmOn(DisposeCancel).ConfigureAwait(false);
                        }
                        else
                        {
                            _svc.Gbs.Position.Value = position;
                            await RtcmOff(DisposeCancel).ConfigureAwait(false);
                        }

                        break;
                    case TMode3Enum.FixedMode:
                        _svc.Gbs.CustomMode.Value = AsvGbsCustomMode.AsvGbsCustomModeFixed;
                        _svc.Gbs.Position.Value = cfgTMode3.Location ?? position;
                        accuracy = cfgTMode3.FixedPosition3DAccuracy;
                        await RtcmOn(DisposeCancel).ConfigureAwait(false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _svc.Gbs.AccuracyMeter.Value = accuracy;
                _svc.Gbs.ObservationSec.Value = (ushort)observations;
            }
            else
            {
                _svc.Gbs.Position.Value = position;
                _logger.ZLogError(
                    $"[{_device?.Client.Id}]: The device did not respond to the request GetCfgTMode3()"
                );
            }

            var navSat = await _device?.Client.GetNavSat(DisposeCancel)!;

            byte gps = 0;
            byte sbas = 0;
            byte galileo = 0;
            byte beidou = 0;
            byte imes = 0;
            byte qzss = 0;
            byte glo = 0;
            if (navSat != null)
            {
                _svc.Gbs.AllSatellites.Value = navSat.NumSvs;

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
            }
            else
            {
                _svc.Gbs.AllSatellites.Value = _pvt.Value?.NumberOfSatellites ?? 0;
                _logger.ZLogError(
                    $"[{_device?.Client.Id}]: The device did not respond to the request GetNavSat()"
                );
            }

            _svc.Gbs.GpsSatellites.Value = gps;
            _svc.Gbs.SbasSatellites.Value = sbas;
            _svc.Gbs.GalSatellites.Value = galileo;
            _svc.Gbs.BeidouSatellites.Value = beidou;
            _svc.Gbs.ImesSatellites.Value = imes;
            _svc.Gbs.QzssSatellites.Value = qzss;
            _svc.Gbs.GlonassSatellites.Value = glo;
        }
        catch
        {
            // ignored
        }
        finally
        {
            Interlocked.Exchange(ref _updateStatusInProgress, 0);
        }
    }

    private async Task<MavResult> StartIdleMode(CancellationToken cancel)
    {
        if (CheckInitAndBeginCall() == false)
        {
            return MavResult.MavResultTemporarilyRejected;
        }

        try
        {
            if (_device == null || _device.IsInit == false)
            {
                _logger.ZLogError($"Unable to set Standalone Mode. Device not found.");
                _svc.StatusText.Error("Unable to set Standalone Mode. Device not found.");
                return MavResult.MavResultFailed;
            }

            await RtcmOff(cancel, true).ConfigureAwait(false);
            await _device
                .Client.Push(
                    new UbxCfgTMode3 { Mode = TMode3Enum.Disabled, IsGivenInLLA = false },
                    cancel: cancel
                )
                .ConfigureAwait(false);
            _svc.Gbs.CustomMode.Value = AsvGbsCustomMode.AsvGbsCustomModeIdle;
            _svc.Gbs.DgpsRate.Value = 0;
            await _device.Client.RebootReceiver(cancel).ConfigureAwait(false);
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.StatusText.Error("GNSS Standalone Mode error");
            _svc.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }

    private async Task<MavResult> StartAutoMode(
        float duration,
        float accuracy,
        CancellationToken cancel
    )
    {
        if (_config.IsEnabledRtk == false)
        {
            _svc.StatusText.Warning("RTK mode commands are disabled by configuration.");
            return MavResult.MavResultDenied;
        }

        if (CheckInitAndBeginCall() == false)
        {
            return MavResult.MavResultTemporarilyRejected;
        }

        try
        {
            if (_device == null || _device.IsInit == false)
            {
                _logger.ZLogError($"Unable to set Auto Mode. Device not found.");
                _svc.StatusText.Error("Unable to set Auto Mode. Device not found.");
                return MavResult.MavResultFailed;
            }

            _svc.StatusText.Info(
                $"Set GNSS Auto Mode (Duration: {duration:F0}, Accuracy: {accuracy:F1})"
            );
            var mode = await _device.Client.GetCfgTMode3(cancel);
            if (mode == null)
            {
                _logger.ZLogError($"Unable to set Auto Mode.");
                _svc.StatusText.Error("Unable to set Auto Mode.");
                throw new Exception("Unable to set Auto Mode.");
            }

            if (mode.Mode == TMode3Enum.SurveyIn)
            {
                await _device
                    .Client.Push(
                        new UbxCfgTMode3 { Mode = TMode3Enum.Disabled, IsGivenInLLA = false },
                        cancel: cancel
                    )
                    .ConfigureAwait(false);
            }
            _svc.StatusText.Info($"Set GNSS AUTO mode (dur:{duration:F0},acc:{accuracy:F0})");
            await _device.Client.SetSurveyInMode((uint)duration, accuracy, cancel);

            if (mode.Mode == TMode3Enum.FixedMode)
            {
                await _device.Client.RebootReceiver(cancel).ConfigureAwait(false);
            }

            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.StatusText.Error("GNSS AUTO mode error");
            _svc.StatusText.Error(e.Message);
            _logger.ZLogError($"{e.Message}.");
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }

    private async Task<MavResult> StartFixedMode(
        GeoPoint geoPoint,
        float accuracy,
        CancellationToken cancel
    )
    {
        if (_config.IsEnabledRtk == false)
        {
            _svc.StatusText.Warning("RTK mode commands are disabled by configuration.");
            return MavResult.MavResultDenied;
        }

        if (CheckInitAndBeginCall() == false)
        {
            return MavResult.MavResultTemporarilyRejected;
        }

        try
        {
            if (_device == null || _device.IsInit == false)
            {
                _logger.ZLogError($"Unable to set Fixed Mode. Device not found.");
                _svc.StatusText.Error("Unable to set Fixed Mode. Device not found.");
                return MavResult.MavResultFailed;
            }

            if (
                RtkFixedModeValidator.TryValidate(geoPoint, accuracy, out var validationError)
                == false
            )
            {
                _logger.ZLogError($"Unable to set Fixed Mode. {validationError}");
                _svc.StatusText.Error(validationError);
                return MavResult.MavResultDenied;
            }

            _svc.StatusText.Info($"Set GNSS Fixed Mode ({geoPoint})");
            await _device.Client.SetFixedBaseMode(geoPoint, accuracy, cancel).ConfigureAwait(false);
            if (await _device.WaitForFixedMode(cancel).ConfigureAwait(false) == false)
            {
                _svc.StatusText.Error("GNSS Fixed Mode error: TMODE3 was not applied.");
                return MavResult.MavResultFailed;
            }

            await RtcmOn(cancel, true).ConfigureAwait(false);
            return MavResult.MavResultAccepted;
        }
        catch (Exception e)
        {
            _svc.StatusText.Error("GNSS Fixed Mode error");
            _svc.StatusText.Error(e.Message);
            return MavResult.MavResultFailed;
        }
        finally
        {
            EndCall();
        }
    }

    private async Task TryConnectToDevice()
    {
        _connToDevSub?.Dispose();
        _connToDevSub = null;
        _svc.StatusText.Info($"Try to configure port for device ({_config.ConnectionString})...");
        if (await _connections.SetUpConnection(_config.ConnectionString).ConfigureAwait(false))
        {
            _router.AddPort(_config.ConnectionString);
        }
        else
        {
            _svc.StatusText.Info(
                $"Failed to configure port. Next attempt in {_config.ReconnectTimeoutMs / 1000} seconds."
            );
            _connToDevSub = Observable
                .Timer(TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs), DisposeCancel)
                .Subscribe(_ => TryConnectToDevice().SafeFireAndForget());
        }
    }

    public Task StartAsync(CancellationToken cancel)
    {
        // if disabled => do nothing
        if (_config.IsEnabled == false)
        {
            return Task.CompletedTask;
        }

        _connToDevSub = Observable
            .Timer(TimeSpan.FromMilliseconds(100), DisposeCancel)
            .Subscribe(_ => TryConnectToDevice().SafeFireAndForget());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancel)
    {
        UnsubscribeFromPreviousDevice();
        return Task.CompletedTask;
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
            _svc.StatusText.Warning("Temporarily rejected: now is busy");
            return false;
        }

        if (_device == null || _device.IsInit == false)
        {
            EndCall();
            _svc.StatusText.Warning("Temporarily rejected: GNSS device is not initialized");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ends the call and updates the '_busy' flag to indicate that the call has ended.
    /// </summary>
    private void EndCall()
    {
        Interlocked.Exchange(ref _busy, 0);
    }

    /// <summary>
    /// Initializes the UBX GNSS device.
    /// </summary>
    private async void InitUbxDevice(UbxRtkDevice device)
    {
        try
        {
            UnsubscribeFromPreviousDevice();
            _navSvInSubscription = device.NavSvIn.Subscribe(msg => _svIn.Value = msg);
            _navPvtSubscription = device.NavPvt.Subscribe(msg => _pvt.Value = msg);
            if (_config.IsEnabledRtk)
            {
                _rtcmV3Subscription = device.RtcmV3Message.Subscribe(msg =>
                    SendRtcm(msg).SafeFireAndForget()
                );
            }
            await device.Init(_svc, _router).ConfigureAwait(false);
            _areRtcmSending = device.AreRtcmSending;

            if (_config.IsEnabledRtk == false)
            {
                _svc.Gbs.DgpsRate.Value = 0;
            }
        }
        catch
        {
            // ignored
        }
    }

    private void UnsubscribeFromPreviousDevice()
    {
        try
        {
            _navSvInSubscription?.Dispose();
            _navPvtSubscription?.Dispose();
            _rtcmV3Subscription?.Dispose();
            _navSvInSubscription = null;
            _navPvtSubscription = null;
            _rtcmV3Subscription = null;
        }
        catch
        {
            // ignored
        }
    }
}

sealed class UbxRtkDevice : AsyncDisposableWithCancel
{
    private IDisposable? _initUbxSub = null;
    private readonly RtkDeviceOptions _config;
    private readonly ReactiveProperty<UbxNavSvin> _navSvIn = new();
    private readonly ReactiveProperty<UbxNavPvt> _navPvt = new();
    private readonly ReactiveProperty<RtcmV3MessageBase> _rtcmV3Message = new();
    public IUbxMicroserviceClient Client { get; }
    public IClientDevice Device { get; }
    public bool IsInit { get; private set; }

    public UbxRtkDevice(
        IUbxMicroserviceClient client,
        IClientDevice device,
        RtkDeviceOptions config
    )
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Device = device ?? throw new ArgumentNullException(nameof(device));
        DisposeCancel.Register(() =>
        {
            IsInit = false;
            _initUbxSub?.Dispose();
            _initUbxSub = null;
            _navSvIn.Dispose();
            _navPvt.Dispose();
            _rtcmV3Message.Dispose();
        });
    }

    public ReadOnlyReactiveProperty<UbxNavSvin> NavSvIn => _navSvIn;
    public ReadOnlyReactiveProperty<UbxNavPvt> NavPvt => _navPvt;
    public ReadOnlyReactiveProperty<RtcmV3MessageBase> RtcmV3Message => _rtcmV3Message;
    public bool AreRtcmSending { get; set; }

    public async Task Init(IMavlinkService svc, IProtocolRouter router)
    {
        try
        {
            _initUbxSub?.Dispose();
            _initUbxSub = null;
            svc.Gbs.CustomMode.Value = AsvGbsCustomMode.AsvGbsCustomModeLoading;
            var ver = await Client.GetMonVer();
            svc.StatusText.Debug($"Found GNSS HW:{ver?.Hardware.Trim('\0')}");
            svc.StatusText.Debug($"GNSS SW:{ver?.Software.Trim('\0')}");
            var ext = ver?.Extensions.Select(_ => _.Trim('\0')).Distinct().ToArray();
            svc.StatusText.Debug($"GNSS EXT:{string.Join(",", ext ?? [])}");
            await Client.SetStationaryMode(false, _config.MessageRateHz);
            await Client.TurnOffNmea(CancellationToken.None);
            await Client.SetMessageRate<UbxNavSvin>(_config.MessageRateHz); // surveyin msg - for feedback
            await Client.SetMessageRate<UbxNavPvt>(_config.MessageRateHz); // pvt msg - for feedback
            await SetRtcmOutput(false, DisposeCancel).ConfigureAwait(false);

            // NAV-VELNED - 1s
            await Client.SetMessageRate(
                (byte)UbxProtocol.ClassIDs.NAV,
                0x12,
                _config.MessageRateHz
            );

            // rxm-raw/rawx - 1s
            await Client.SetMessageRate(
                (byte)UbxProtocol.ClassIDs.RXM,
                0x15,
                _config.MessageRateHz
            );

            // await SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x10, 1, cancel);
            // rxm-sfrb/sfrb - 2s
            await Client.SetMessageRate((byte)UbxProtocol.ClassIDs.RXM, 0x13, 2, default);

            // await SetMessageRate((byte)UbxHelper.ClassIDs.RXM, 0x11, 2, cancel);
            // mon-hw - 2s
            await Client.SetMessageRate((byte)UbxProtocol.ClassIDs.MON, 0x09, 2, default);

            router
                .RxFilterByType<UbxNavSvin>()
                .Subscribe(msg => _navSvIn.OnNext(msg))
                .RegisterTo(DisposeCancel);
            router
                .RxFilterByType<UbxNavPvt>()
                .Subscribe(msg => _navPvt.OnNext(msg))
                .RegisterTo(DisposeCancel);
            if (_config.IsEnabledRtk)
            {
                var rtcmV3Filter = new HashSet<ushort>(_config.RtcmV3MessagesIdsToSend);
                router
                    .RxFilterByType<RtcmV3MessageBase>()
                    .Where(msg => rtcmV3Filter.Contains(msg.Id))
                    .Subscribe(msg => _rtcmV3Message.OnNext(msg))
                    .RegisterTo(DisposeCancel);
            }

            svc.Gbs.CustomMode.Value = AsvGbsCustomMode.AsvGbsCustomModeIdle;

            await AutoStartFixedMode(svc).ConfigureAwait(false);
            IsInit = true;
            _initUbxSub?.Dispose();
            _initUbxSub = null;
        }
        catch
        {
            // _svc.Server.StatusText.Debug(e.Message);
            svc.StatusText.Error("Error to init GNSS");
            svc.StatusText.Debug(
                $"Reconnect after {TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs).TotalSeconds:F0} sec..."
            );
            svc.Gbs.CustomMode.Value = AsvGbsCustomMode.AsvGbsCustomModeError;
            _initUbxSub = Observable
                .Timer(TimeSpan.FromMilliseconds(_config.ReconnectTimeoutMs))
                .Subscribe(_ => Init(svc, router).SafeFireAndForget());
        }
    }

    private async Task AutoStartFixedMode(IMavlinkService svc)
    {
        if (_config.IsEnabledRtk == false || _config.IsAutoStartFixedMode == false)
        {
            return;
        }

        var position = new GeoPoint(
            _config.FixedModeLat,
            _config.FixedModeLon,
            _config.FixedModeAlt
        );
        if (
            RtkFixedModeValidator.TryValidate(
                position,
                _config.FixedModeAccuracy,
                out var validationError
            ) == false
        )
        {
            svc.StatusText.Error($"GNSS Fixed Mode auto start skipped. {validationError}");
            return;
        }

        try
        {
            svc.StatusText.Info($"Auto start GNSS Fixed Mode ({position})");
            await Client
                .SetFixedBaseMode(position, _config.FixedModeAccuracy, DisposeCancel)
                .ConfigureAwait(false);
            if (await WaitForFixedMode(DisposeCancel).ConfigureAwait(false) == false)
            {
                svc.StatusText.Error("GNSS Fixed Mode auto start error: TMODE3 was not applied.");
                return;
            }

            await SetRtcmOutput(true, DisposeCancel).ConfigureAwait(false);
            svc.Gbs.CustomMode.Value = AsvGbsCustomMode.AsvGbsCustomModeFixed;
            svc.Gbs.Position.Value = position;
            svc.Gbs.AccuracyMeter.Value = _config.FixedModeAccuracy;
            svc.Gbs.ObservationSec.Value = 0;
        }
        catch (Exception e)
        {
            svc.StatusText.Error("GNSS Fixed Mode auto start error");
            svc.StatusText.Error(e.Message);
        }
    }

    public async Task<bool> WaitForFixedMode(CancellationToken cancel)
    {
        for (var i = 0; i < 10; i++)
        {
            cancel.ThrowIfCancellationRequested();
            var mode = await Client.GetCfgTMode3(cancel).ConfigureAwait(false);
            if (mode?.Mode == TMode3Enum.FixedMode)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancel).ConfigureAwait(false);
        }

        return false;
    }

    public async Task SetRtcmOutput(bool enabled, CancellationToken cancel)
    {
        if (enabled && _config.IsEnabledRtk)
        {
            await Client
                .SetMessageRate((byte)UbxProtocol.ClassIDs.RTCM3, 0x05, 5)
                .ConfigureAwait(false); // 1005 - 5s
            await Client.SetupRtcmMSM4Rate(_config.MessageRateHz, cancel).ConfigureAwait(false);
            await Client
                .SetMessageRate((byte)UbxProtocol.ClassIDs.RTCM3, 0xE6, 5)
                .ConfigureAwait(false); // 1230 - 5s
            AreRtcmSending = true;
            return;
        }

        await Client
            .SetMessageRate((byte)UbxProtocol.ClassIDs.RTCM3, 0x05, 0)
            .ConfigureAwait(false);
        await Client.SetupRtcmMSM4Rate(0, cancel).ConfigureAwait(false);
        await Client
            .SetMessageRate((byte)UbxProtocol.ClassIDs.RTCM3, 0xE6, 0)
            .ConfigureAwait(false);
        AreRtcmSending = false;
    }
}
