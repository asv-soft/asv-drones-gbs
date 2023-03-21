using System.ComponentModel.Composition;
using Asv.Common;
using Asv.Drones.Gbs.Core;

namespace Asv.Drones.Station
{
    
    

    [Export(typeof(IModule))]
    [PartCreationPolicy(CreationPolicy.Shared)]
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
