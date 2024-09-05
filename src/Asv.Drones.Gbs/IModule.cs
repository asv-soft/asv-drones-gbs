namespace Asv.Drones.Gbs;

/// <summary>
/// Represents a module that can be initialized and disposed.
/// </summary>
public interface IModule:IDisposable
{
    /// <summary>
    /// Initializes the module.
    /// </summary>
    void Init();
}