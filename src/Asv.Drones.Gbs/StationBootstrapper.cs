using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using Asv.Cfg;
using Asv.Drones.Gbs.Core;
using Asv.Mavlink;
using NLog;

namespace Asv.Drones.Gbs;

public class StationBootstrapper
{
    private readonly IConfiguration _config;
    private CompositionContainer _container;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private IModule[] _modules;

    public StationBootstrapper(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _container = new CompositionContainer(new AggregateCatalog(RegisterAssembly.Distinct().Select(_ => new AssemblyCatalog(_)).OfType<ComposablePartCatalog>()));
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
                module.Init();
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
            yield return typeof(StationBootstrapper).Assembly;
            yield return typeof(IModule).Assembly; // Core
        }
    }

    
}
