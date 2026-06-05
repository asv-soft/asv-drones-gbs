using System.Diagnostics.Metrics;
using Asv.Common;
using Asv.Gnss;
using Asv.IO;
using Asv.Mavlink;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.Drones.Gbs;

public interface IDeviceConnectionsService
{
    Task<bool> SetUpConnection(string connectionString);
}

public class UBloxDeviceConnectionsService : AsyncDisposableWithCancel, IDeviceConnectionsService
{
    private readonly IMavlinkService _svc;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMeterFactory _meterFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UBloxDeviceConnectionsService> _logger;
    private readonly Subject<UbxMessageBase> _internalFilteredDeviceMessages = new();
    private readonly GnssDeviceId _deviceId = new("UBX");
    private string _endpointId;

    public UBloxDeviceConnectionsService(
        IMavlinkService mavlink,
        ILoggerFactory loggerFactory,
        IMeterFactory meterFactory,
        TimeProvider timeProvider
    )
    {
        _svc = mavlink;
        _loggerFactory = loggerFactory;
        _meterFactory = meterFactory;
        _timeProvider = timeProvider;
        _logger = loggerFactory.CreateLogger<UBloxDeviceConnectionsService>();
    }

    private delegate bool FilterDelegate<TResult, in TMessage>(
        TMessage inputPacket,
        out TResult result
    )
        where TMessage : UbxMessageBase;

    private bool FilterDeviceMessages(UbxMessageBase arg)
    {
        var endpointId = arg.GetEndpointId();
        if (endpointId == null)
        {
            return false;
        }
        return _endpointId == endpointId;
    }

    private async Task Push<T>(
        IProtocolPort port,
        T packet,
        int timeoutMs = 1000,
        int attemptCount = 5,
        CancellationToken cancel = default
    )
        where T : UbxMessageBase, new()
    {
        FilterDelegate<(UbxAckAck?, UbxAckNak?), UbxMessageBase> filter = Filter;
        var result = await InternalCall(port, packet, filter, attemptCount, null, timeoutMs, cancel)
            .ConfigureAwait(false);
        if (result.Item2 != null)
        {
            throw new NotSupportedException(
                $"[{_deviceId.AsString()}] Error pushing {packet.Name}"
            );
        }
        return;

        bool Filter(UbxMessageBase inputPacket, out (UbxAckAck?, UbxAckNak?) resultPacket)
        {
            switch (inputPacket)
            {
                case UbxAckAck ackAck
                    when ackAck.AckClassId == packet.Class
                        && ackAck.AckSubclassId == packet.SubClass:
                    resultPacket = (ackAck, null);
                    return true;
                case UbxAckNak ackNak
                    when ackNak.AckClassId == packet.Class
                        && ackNak.AckSubclassId == packet.SubClass:
                    resultPacket = (null, ackNak);
                    return true;
                default:
                    resultPacket = (null, null);
                    return false;
            }
        }
    }

    private async Task<TPacket?> Pool<TPacket, TPoolPacket>(
        IProtocolPort port,
        TPoolPacket packet,
        int timeoutMs = 1000,
        int attemptCount = 5,
        CancellationToken cancel = default
    )
        where TPacket : UbxMessageBase
        where TPoolPacket : UbxMessageBase, new()
    {
        FilterDelegate<(TPacket?, UbxAckNak?), UbxMessageBase> filter = Filter;
        var result = await InternalCall(port, packet, filter, attemptCount, null, timeoutMs, cancel)
            .ConfigureAwait(false);
        if (result.Item2 != null)
        {
            throw new NotSupportedException(
                $"[{_deviceId.AsString()}] Error pushing {packet.Name}"
            );
        }
        return result.Item1;

        bool Filter(UbxMessageBase inputPacket, out (TPacket?, UbxAckNak?) resultPacket)
        {
            switch (inputPacket)
            {
                case TPacket pkt when pkt.Class == packet.Class && pkt.SubClass == packet.SubClass:
                    resultPacket = (pkt, null);
                    return true;
                case UbxAckNak ackNak
                    when ackNak.AckClassId == packet.Class
                        && ackNak.AckSubclassId == packet.SubClass:
                    resultPacket = (null, ackNak);
                    return true;
                default:
                    resultPacket = (null, null);
                    return false;
            }
        }
    }

    private async Task<TResult> InternalCall<TResult, TSend, TReceive>(
        IProtocolPort port,
        TSend packet,
        FilterDelegate<TResult, TReceive> filterAndResultGetter,
        int attemptCount = 5,
        Action<TSend, int>? fillOnConfirmation = null,
        int timeoutMs = 1000,
        CancellationToken cancel = default
    )
        where TSend : UbxMessageBase, new()
        where TReceive : UbxMessageBase
    {
        cancel.ThrowIfCancellationRequested();
        byte currentAttempt = 0;
        var name = packet.Name;
        while (IsRetryCondition())
        {
            if (currentAttempt != 0)
            {
                fillOnConfirmation?.Invoke(packet, currentAttempt);
                _logger.ZLogWarning($"=> replay {currentAttempt} {name}");
            }
            ++currentAttempt;
            try
            {
                return await InternalSendAndWaitAnswer(
                        port,
                        packet,
                        filterAndResultGetter,
                        timeoutMs,
                        cancel
                    )
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (IsRetryCondition())
                {
                    continue;
                }

                cancel.ThrowIfCancellationRequested();
            }
        }
        _logger.ZLogError($"Timeout to execute '{name}' with {attemptCount} x {timeoutMs} ms'");
        throw new TimeoutException(
            $"Timeout to execute '{name}' with {attemptCount} x {timeoutMs} ms'"
        );
        bool IsRetryCondition() => currentAttempt < attemptCount;
    }

    private async Task<TResult> InternalSendAndWaitAnswer<TResult, TMessage>(
        IProtocolPort port,
        UbxMessageBase packet,
        FilterDelegate<TResult, TMessage> filterAndResultGetter,
        int timeoutMs = 1000,
        CancellationToken cancel = default
    )
        where TMessage : UbxMessageBase
    {
        cancel.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(filterAndResultGetter);
        _logger.ZLogTrace(
            $"=> Send {packet.Name} and wait for answer {nameof(TResult)} with timeout {timeoutMs} ms"
        );
        using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(
            cancel,
            DisposeCancel
        );
        linkedCancel.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs), _timeProvider);
        var tcs = new TaskCompletionSource<TResult>();
        await using var c1 = linkedCancel.Token.Register(() => tcs.TrySetCanceled(), false);

        using var subscribe = InternalFilteredDeviceMessages.Subscribe(x =>
        {
            if (x is TMessage msg)
            {
                if (filterAndResultGetter(msg, out var result))
                {
                    tcs.TrySetResult(result);
                }
            }
        });
        await port.Send(packet, linkedCancel.Token).ConfigureAwait(false);
        var result = await tcs.Task.ConfigureAwait(false);
        _logger.ZLogTrace($"<= ok {packet.Name}<=={result}");
        return result;
    }

    private Observable<UbxMessageBase> InternalFilteredDeviceMessages =>
        _internalFilteredDeviceMessages;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.ZLogTrace($"Dispose {nameof(UBloxDeviceConnectionsService)}");
            _internalFilteredDeviceMessages.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_internalFilteredDeviceMessages);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    #endregion


    public async Task<bool> SetUpConnection(string connectionString)
    {
        try
        {
            if (!IsSerialPort(connectionString, out var baudRate))
            {
                return true;
            }

            IProtocolRouter? router = null;
            IDisposable? sub1 = null;
            try
            {
                var factory = Protocol.Create(builder =>
                {
                    builder.SetLog(_loggerFactory);
                    builder.SetMetrics(_meterFactory);
                    builder.Features.RegisterBroadcastAllFeature();
                    builder.Features.RegisterEndpointIdTagFeature();
                    builder.Protocols.RegisterUbxProtocol();
                });

                router = factory.CreateRouter("UBX");

                sub1 = router
                    .RxFilterByType<UbxMessageBase>()
                    .Where(FilterDeviceMessages)
                    .Subscribe(_internalFilteredDeviceMessages.AsObserver());

                await ConfigureBaudRate(router, connectionString, baudRate).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                sub1?.Dispose();
                if (router != null)
                {
                    await router.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string UpdateSerialPortBaudRate(string connectionString, int baudRate)
    {
        var uri = new Uri(connectionString);
        return $"{uri.Scheme}:{uri.LocalPath}?br={baudRate}";
    }

    private bool IsSerialPort(string connectionString, out int baudRate)
    {
        var uri = new Uri(connectionString);
        if (SerialPortConfig.TryParseFromUri(uri, out SerialPortConfig? opt))
        {
            baudRate = opt?.BoundRate ?? 115200;
            return true;
        }
        baudRate = 0;
        return false;
    }

    /// <summary>
    /// Configures the baud rate a UbxDevice.
    /// </summary>
    /// <param name="router">router.</param>
    /// <param name="connectionString">connection string.</param>
    /// <param name="requiredBoundRate">required bound rate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the UbxDevice.</returns>
    private async Task ConfigureBaudRate(
        IProtocolRouter router,
        string connectionString,
        int requiredBoundRate
    )
    {
        var availableBr = new[] { requiredBoundRate, 9600, 38400, 57600, 115200, 230400, 460800 }
            .Distinct()
            .ToArray();
        Exception? lastEx = null;
        IProtocolPort? port = null;
        foreach (var br in availableBr)
        {
            // UbxDevice? device = null;
            // CustomSerialPort? port = null;
            IDisposable? endPointAddedSub = null;
            try
            {
                connectionString = UpdateSerialPortBaudRate(connectionString, br);
                _logger.ZLogTrace($"=> Configure baud rate {br}. '{connectionString}'");

                port = router.AddPort(connectionString);
                endPointAddedSub = port.EndpointAdded.Subscribe(x => _endpointId = x.Id);
                await Task.Delay(500, DisposeCancel).ConfigureAwait(false);

                var cfg1 = await GetCfgPort(port, 1, DisposeCancel).ConfigureAwait(false);
                if (cfg1 is null)
                {
                    throw new Exception("Can't get config port");
                }

                var cfgPort = (UbxCfgPrtConfigUart)cfg1.Config;
                _svc.StatusText.Info($"GNSS device BoundRate: {cfgPort.BoundRate}");
                if (cfgPort.BoundRate == requiredBoundRate)
                {
                    return;
                }

                _svc.StatusText.Info(
                    $"Change BoundRate {cfgPort.BoundRate} => {requiredBoundRate}"
                );
                await SetCfgPort(
                        port,
                        new UbxCfgPrt
                        {
                            Config = new UbxCfgPrtConfigUart
                            {
                                PortId = 1,
                                BoundRate = requiredBoundRate,
                            },
                        },
                        DisposeCancel
                    )
                    .ConfigureAwait(false);

                endPointAddedSub.Dispose();
                endPointAddedSub = null;

                router.RemovePort(port);
                await Task.Delay(500, DisposeCancel).ConfigureAwait(false);

                connectionString = UpdateSerialPortBaudRate(connectionString, requiredBoundRate);
                port = router.AddPort(connectionString);
                endPointAddedSub = port.EndpointAdded.Subscribe(x => _endpointId = x.Id);
                await Task.Delay(500, DisposeCancel).ConfigureAwait(false);

                var cfg2 = await GetCfgPort(port, 1, DisposeCancel).ConfigureAwait(false);
                if (cfg2 is null)
                {
                    throw new Exception("Can't get config port");
                }

                cfgPort = (UbxCfgPrtConfigUart)cfg2.Config;
                _svc.StatusText.Info($"GNSS device BoundRate: {cfgPort.BoundRate}");
                endPointAddedSub.Dispose();
                if (cfgPort.BoundRate == requiredBoundRate)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                _logger.ZLogTrace($"=> Error to configure baud rate: {e.Message}");
                endPointAddedSub?.Dispose();
                if (port != null)
                {
                    router.RemovePort(port);
                    await Task.Delay(500, DisposeCancel).ConfigureAwait(false);
                }
                lastEx = e;
            }
        }

        _logger.ZLogTrace($"=> Error configure baud rate: {lastEx?.Message}");
        throw lastEx!;
    }

    /// <summary>
    /// Retrieves the configuration port for the specified port ID.
    /// </summary>
    /// <param name="port">port.</param>
    /// <param name="portId">The port ID to retrieve the configuration port for.</param>
    /// <param name="cancel">A CancellationToken to cancel the operation (optional).</param>
    /// <returns>A Task representing the operation, which will return an instance of UbxCfgPrt.</returns>
    private Task<UbxCfgPrt?> GetCfgPort(
        IProtocolPort port,
        byte portId,
        CancellationToken cancel = default
    )
    {
        return Pool<UbxCfgPrt, UbxCfgPrtPool>(
            port,
            new UbxCfgPrtPool { PortId = portId },
            cancel: cancel
        );
    }

    /// <summary>
    /// Sets the configuration port of the IUbxDevice object.
    /// </summary>
    /// <param name="port">port.</param>
    /// <param name="value">The configuration port value to set.</param>
    /// <param name="cancel">The cancellation token (optional).</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    private Task SetCfgPort(IProtocolPort port, UbxCfgPrt value, CancellationToken cancel = default)
    {
        return Push(port, value, cancel: cancel);
    }
}
