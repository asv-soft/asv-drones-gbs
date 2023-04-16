using System.ComponentModel.Composition;
using Asv.Common;

namespace Asv.Drones.Gbs
{
    
    

    // [Export(typeof(IModule))]
    // [PartCreationPolicy(CreationPolicy.Shared)] // [Export(typeof(IModule))]
    // [PartCreationPolicy(CreationPolicy.Shared)]
    public class MavlinkActivationModule : DisposableOnceWithCancel, IModule
    {

        [ImportingConstructor]
        public MavlinkActivationModule(IGbsMavlinkService svc)
        {
            
        }

        public void Init()
        {

        }
    }
}
