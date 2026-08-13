using Asv.Cfg;
using Asv.Drones.Plugin.Gbs;

namespace Asv.Drones.Gbs.Tests;

public class ConfigurationRoundTripTests
{
    [Fact]
    public void FixedModeSavedCoords_ShouldRoundTripThroughAsvConfiguration()
    {
        using var configuration = new InMemoryConfiguration();
        var original = new FixedModeSavedCoords
        {
            Coords =
            [
                new FixedModeConfig
                {
                    Name = "Roof",
                    Latitude = 56.8389,
                    Longitude = 60.6057,
                    Altitude = 281.4,
                    Accuracy = 0.015,
                },
                new FixedModeConfig
                {
                    Name = "Field",
                    Latitude = -33.865143,
                    Longitude = 151.2099,
                    Altitude = 12.5,
                    Accuracy = 0.5,
                },
            ],
        };

        configuration.Set(original);
        var restored = configuration.Get<FixedModeSavedCoords>();

        Assert.Collection(
            restored.Coords,
            item => AssertFixedModeConfigEqual(original.Coords[0], item),
            item => AssertFixedModeConfigEqual(original.Coords[1], item)
        );
    }

    [Fact]
    public void AutoModeConfig_ShouldRoundTripThroughAsvConfiguration()
    {
        using var configuration = new InMemoryConfiguration();
        var original = new AutoModeConfig { Accuracy = 0.25, Observation = 180 };

        configuration.Set(original);
        var restored = configuration.Get<AutoModeConfig>();

        Assert.Equal(original.Accuracy, restored.Accuracy);
        Assert.Equal(original.Observation, restored.Observation);
    }

    private static void AssertFixedModeConfigEqual(FixedModeConfig expected, FixedModeConfig actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Latitude, actual.Latitude);
        Assert.Equal(expected.Longitude, actual.Longitude);
        Assert.Equal(expected.Altitude, actual.Altitude);
        Assert.Equal(expected.Accuracy, actual.Accuracy);
    }
}
