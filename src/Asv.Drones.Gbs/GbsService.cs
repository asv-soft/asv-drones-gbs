using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using Asv.Cfg;
using Asv.Common;
using Asv.Drones.Gbs.Core;
using Asv.Drones.Gbs.Ublox;
using Asv.Mavlink;
using NLog;

namespace Asv.Drones.Gbs;

internal class GbsService : DisposableOnceWithCancel
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IConfiguration _config;
    private readonly CompositionContainer _container;
    private readonly IModule[] _modules;

    public GbsService(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _container = new CompositionContainer(new AggregateCatalog(
            RegisterAssembly.Distinct().Select(_ => new AssemblyCatalog(_)).OfType<ComposablePartCatalog>()))
            .DisposeItWith(Disposable);
        var batch = new CompositionBatch();
        batch.AddExportedValue<IConfiguration>(_config);
        batch.AddExportedValue<IPacketSequenceCalculator>(new PacketSequenceCalculator());
        batch.AddExportedValue(_container);
        _container.Compose(batch);
        
        _modules = _container.GetExportedValues<IModule>().ToArray();
        _logger.Info($"Begin loading modules [{_modules.Length} items]");
        foreach (var module in _modules)
        {
            try
            {
                _logger.Trace($"Init {module.GetType().Name}");
                module.Init();
                module.DisposeItWith(Disposable);
            }
            catch (Exception e)
            {
                _logger.Error($"Error to init module '{module}':{e.Message}");
                throw;
            }
        }
        
    }
    private IEnumerable<Assembly> RegisterAssembly
    {
        get
        {
            yield return typeof(GbsService).Assembly;           // [this]
            yield return typeof(IModule).Assembly;              // Core
            yield return typeof(UbloxRtkModule).Assembly;       // Ublox
        }
    }
}