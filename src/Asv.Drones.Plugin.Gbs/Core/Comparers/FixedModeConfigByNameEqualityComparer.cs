namespace Asv.Drones.Plugin.Gbs;

public class FixedModeConfigByNameEqualityComparer : IEqualityComparer<FixedModeConfig>
{
    public static readonly FixedModeConfigByNameEqualityComparer Instance = new();

    public bool Equals(FixedModeConfig? x, FixedModeConfig? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        if (x is null)
        {
            return false;
        }
        if (y is null)
        {
            return false;
        }
        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Name == y.Name;
    }

    public int GetHashCode(FixedModeConfig obj)
    {
        return obj.Name?.GetHashCode() ?? 0;
    }
}
