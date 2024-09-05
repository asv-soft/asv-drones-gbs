using System.ComponentModel.Composition;
using Asv.Common;

namespace Asv.Drones.Gbs
{
    
    

    // [Export(typeof(IModule))]
    // [PartCreationPolicy(CreationPolicy.Shared)] // [Export(typeof(IModule))]
    // [PartCreationPolicy(CreationPolicy.Shared)]
    /// <summary>
    /// Represents a module responsible for activating Mavlink.
    /// </summary>
    public class MavlinkActivationModule : DisposableOnceWithCancel, IModule
    {
        /// <summary>
        /// Initializes a new instance of the MavlinkActivationModule class.
        /// </summary>
        /// <param name="svc">The GbsMavlinkService instance used for communication.</param>
        [ImportingConstructor]
        public MavlinkActivationModule(IGbsMavlinkService svc)
        {
            
        }

        /// <summary>
        /// Initializes the object.
        /// </summary>
        /// <remarks>
        /// This method is used to initialize the object and prepare it for use.
        /// </remarks>
        public void Init()
        {

        }
    }
}
