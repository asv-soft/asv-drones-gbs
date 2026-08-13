using Asv.Drones.Plugin.Gbs;

namespace Asv.Drones.Gbs.Tests;

public class FixedModeConfigByNameEqualityComparerTests
{
    private readonly FixedModeConfigByNameEqualityComparer _sut =
        FixedModeConfigByNameEqualityComparer.Instance;

    [Fact]
    public void Equals_ShouldUseOnlyExactNameForSameRuntimeType()
    {
        var left = Create("Base A", latitude: 10);
        var right = Create("Base A", latitude: 50);

        Assert.True(_sut.Equals(left, right));
        Assert.Equal(_sut.GetHashCode(left), _sut.GetHashCode(right));
        Assert.False(_sut.Equals(left, Create("base a", latitude: 10)));
        Assert.False(_sut.Equals(left, Create("Base B", latitude: 10)));
    }

    [Fact]
    public void Equals_ShouldHandleReferencesAndNulls()
    {
        var value = Create("Base A", latitude: 10);

        Assert.True(_sut.Equals(value, value));
        Assert.True(_sut.Equals(null, null));
        Assert.False(_sut.Equals(value, null));
        Assert.False(_sut.Equals(null, value));
    }

    [Fact]
    public void Equals_ShouldRejectDifferentRuntimeTypes()
    {
        var baseValue = Create("Base A", latitude: 10);
        var derivedValue = new DerivedFixedModeConfig { Name = "Base A", Latitude = 10 };

        Assert.False(_sut.Equals(baseValue, derivedValue));
    }

    [Fact]
    public void GetHashCode_ShouldReturnZeroForNullName()
    {
        var value = Create(null!, latitude: 10);

        Assert.Equal(0, _sut.GetHashCode(value));
    }

    private static FixedModeConfig Create(string name, double latitude)
    {
        return new FixedModeConfig
        {
            Name = name,
            Latitude = latitude,
            Longitude = 20,
            Altitude = 30,
            Accuracy = 0.1,
        };
    }

    private sealed class DerivedFixedModeConfig : FixedModeConfig;
}
