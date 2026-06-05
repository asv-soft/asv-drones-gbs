using Asv.Mavlink;

namespace Asv.Drones.Gbs.Contracts;

public interface IMavParamsSource
{
    IEnumerable<IMavParamTypeMetadata> Params { get; }
}
