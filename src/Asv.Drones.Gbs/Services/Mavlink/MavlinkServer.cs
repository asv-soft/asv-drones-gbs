using System.Diagnostics.Metrics;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Gbs.Contracts;
using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.Common;
using Asv.Mavlink.Diagnostic.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using R3;
using ZLogger;

namespace Asv.Drones.Gbs;

public class MavlinkServer : AsyncDisposableOnce, IMavlinkService
{
    private readonly IDisposable _disposeIt;

    public MavlinkServer(
        IOptions<MavlinkServerOptions> options,
        IMavParamsSource paramsSource,
        ILoggerFactory loggerFactory,
        IMeterFactory meterFactory,
        TimeProvider timeProvider,
        IConfiguration userConfig
    )
    {
        var logger = loggerFactory.CreateLogger<MavlinkServer>();
        var msgFactory = MavlinkV2Protocol.CreateMessageFactory();
        var systemId = MavParams.BrdSysId.ReadFromConfig(
            userConfig,
            options.Value.Params.CfgPrefix
        );
        var componentId = MavParams.BrdComId.ReadFromConfig(
            userConfig,
            options.Value.Params.CfgPrefix
        );
        var wrapToV2Extension =
            (byte)MavParams.BrdV2extOn.ReadFromConfig(userConfig, options.Value.Params.CfgPrefix)
            != 0;
        var builder = Disposable.CreateBuilder();

        var protocol = Protocol.Create(builder =>
        {
            builder.SetLog(loggerFactory);
            builder.SetMetrics(meterFactory);
            builder.SetTimeProvider(timeProvider);
            builder.RegisterMavlinkV2Protocol(msgFactory);
            if (wrapToV2Extension)
            {
                builder.Features.RegisterMavlinkV2WrapFeature(msgFactory);
            }
            builder.Features.RegisterBroadcastFeature<MavlinkV2Message>();
        });

        Router = protocol.CreateRouter("GBS").AddTo(ref builder);
        Identity = new MavlinkIdentity(systemId, componentId);
        foreach (var port in options.Value.Connections)
        {
            logger.ZLogTrace($"Add port {port}");
            Router.AddPort(port);
        }

        var seq = new PacketSequenceCalculator();
        var core = new CoreServices(Router, msgFactory, seq);

        Heartbeat = new HeartbeatServer(Identity, options.Value.Heartbeat, core).AddTo(ref builder);
        StatusText = new StatusTextServer(Identity, options.Value.StatusText, core).AddTo(
            ref builder
        );
        Commands = new CommandServer(Identity, core).AddTo(ref builder);
        CommandLongEx = new CommandLongServerEx(Commands).AddTo(ref builder);
        ParamsBase = new ParamsServer(Identity, core).AddTo(ref builder);
        Params = new ParamsServerEx(
            ParamsBase,
            StatusText,
            paramsSource.Params,
            new MavParamByteWiseEncoding(),
            userConfig,
            options.Value.Params
        ).AddTo(ref builder);
        Diagnostic = new DiagnosticServer(Identity, options.Value.Diagnostic, core).AddTo(
            ref builder
        );

        GbsBase = new AsvGbsServer(Identity, options.Value.Gbs, core).AddTo(ref builder);
        Gbs = new AsvGbsExServer(GbsBase, Heartbeat, CommandLongEx).AddTo(ref builder);
        _disposeIt = builder.Build();
    }

    public AsvGbsServer GbsBase { get; }

    public IDiagnosticServer Diagnostic { get; }
    public IAsvGbsServerEx Gbs { get; }
    public IParamsServerEx Params { get; }
    public IParamsServer ParamsBase { get; }
    public ICommandServerEx<CommandLongPacket> CommandLongEx { get; }
    public ICommandServer Commands { get; }
    public IStatusTextServer StatusText { get; }
    public IHeartbeatServer Heartbeat { get; }
    public MavlinkIdentity Identity { get; }
    public IProtocolRouter Router { get; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposeIt.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposeIt is IAsyncDisposable disposeItAsyncDisposable)
        {
            await disposeItAsyncDisposable.DisposeAsync();
        }
        else
        {
            _disposeIt.Dispose();
        }

        await base.DisposeAsyncCore();
    }
}
