using Asv.Mavlink;

namespace Asv.Drones.Gbs.Contracts;

public partial class MavParams
{
    public const string System = "System";
    public const string Advanced = "Advanced";
    public const string Developer = "Developer";

    public const string BOARD = "BOARD";

    public static IEnumerable<IMavParamTypeMetadata> Filter(
        Func<IMavParamTypeMetadata, bool> predicate
    )
    {
        return Instance.Params.Where(predicate);
    }
}
