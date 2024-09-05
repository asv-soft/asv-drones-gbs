using Asv.Mavlink;

namespace Asv.Drones.Gbs;

/// <summary>
/// Represents a GBS Mavlink service.
/// </summary>
public interface IGbsMavlinkService
{
    /// <summary>
    /// Gets the Mavlink router object.
    /// </summary>
    /// <value>
    /// The Mavlink router object.
    /// </value>
    IMavlinkRouter Router { get; }

    /// <summary>
    /// Gets the GbsServerDevice interface representing the server.
    /// </summary>
    /// <remarks>
    /// This property allows access to the GbsServerDevice object, which is responsible
    /// for managing the communication and interaction with the server.
    /// </remarks>
    /// <returns>An instance of IGbsServerDevice representing the server.</returns>
    IGbsServerDevice Server { get; }
}