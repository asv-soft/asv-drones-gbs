using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using Asv.Cfg;
using Asv.Common;
using Asv.Mavlink;
using NLog;

namespace Asv.Drones.Gbs;

/// <summary>
/// Represents a GbsService class that handles the initialization and loading of modules.
/// </summary>
internal class GbsService : DisposableOnceWithCancel
{
    /// <summary>
    /// The logger object used for logging messages.
    /// </summary>
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Represents the configuration object used throughout the application.
    /// </summary>
    private readonly IConfiguration _config;

    /// <summary>
    /// The CompositionContainer used for managing dependencies and composing objects in the application.
    /// </summary>
    private readonly CompositionContainer _container;

    /// <summary>
    /// Represents an array of modules.
    /// </summary>
    private readonly IModule[] _modules;

    /// <summary>
    /// Represents a service used to initialize and load modules.
    /// </summary>
    /// <remarks>
    /// This service is responsible for initializing modules by calling their <c>Init</c> method and disposing
    /// them when the service is disposed. It uses dependency injection to provide necessary dependencies
    /// to the modules.
    /// </remarks>
    /// <seealso cref="IModule"/>
    /// <seealso cref="IConfiguration"/>
    /// <seealso cref="IPacketSequenceCalculator"/>
    public GbsService(IConfiguration config)
    {
        var a = AppDomain.CurrentDomain.GetAssemblies().ToArray();
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _container = new CompositionContainer(new AggregateCatalog(
                AppDomain.CurrentDomain.GetAssemblies().Distinct().Select(_ => new AssemblyCatalog(_)).OfType<ComposablePartCatalog>()))
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

    /// <summary>
    /// Gets the registered assemblies.
    /// </summary>
    /// <remarks>
    /// Returns an enumeration of assemblies that are registered for the current instance.
    /// </remarks>
    /// <value>
    /// An <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="System.Reflection.Assembly"/> objects representing the registered assemblies.
    /// </value>
    private IEnumerable<Assembly> RegisterAssembly
    {
        get
        {
            yield return this.GetType().Assembly;           // [this]
        }
    }
}