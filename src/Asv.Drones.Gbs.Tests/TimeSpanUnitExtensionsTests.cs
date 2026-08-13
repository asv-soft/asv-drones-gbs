using Asv.Avalonia;
using Asv.Cfg;
using Asv.Drones.Plugin.Gbs;

namespace Asv.Drones.Gbs.Tests;

public class TimeSpanUnitExtensionsTests
{
    private readonly TimeSpanUnit _unit = new(
        new InMemoryConfiguration(),
        [
            new TimeSpanMillisecondUnitItem(),
            new TimeSpanSecondUnitItem(),
            new TimeSpanMinuteSecondUnitItem(),
            new TimeSpanHourMinuteSecondUnitItem(),
        ]
    );

    [Theory]
    [InlineData(0)]
    [InlineData(0.999)]
    [InlineData(-0.999)]
    public void GetRelativeTimeUnitItem_ShouldUseMillisecondsBelowOneSecond(double seconds)
    {
        Assert.IsType<TimeSpanMillisecondUnitItem>(_unit.GetRelativeTimeUnitItem(seconds));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(59.999)]
    [InlineData(-59.999)]
    public void GetRelativeTimeUnitItem_ShouldUseSecondsBelowOneMinute(double seconds)
    {
        Assert.IsType<TimeSpanSecondUnitItem>(_unit.GetRelativeTimeUnitItem(seconds));
    }

    [Theory]
    [InlineData(60)]
    [InlineData(-60)]
    [InlineData(3599.999)]
    [InlineData(-3599.999)]
    public void GetRelativeTimeUnitItem_ShouldUseMinutesBelowOneHour(double seconds)
    {
        Assert.IsType<TimeSpanMinuteSecondUnitItem>(_unit.GetRelativeTimeUnitItem(seconds));
    }

    [Theory]
    [InlineData(3600)]
    [InlineData(-3600)]
    [InlineData(86400)]
    public void GetRelativeTimeUnitItem_ShouldUseHoursAtAndAboveOneHour(double seconds)
    {
        Assert.IsType<TimeSpanHourMinuteSecondUnitItem>(_unit.GetRelativeTimeUnitItem(seconds));
    }

    [Fact]
    public void PrintFromSiWithUnitsInRelativeTime_ShouldAppendSelectedUnitSymbol()
    {
        var item = _unit.GetRelativeTimeUnitItem(60);

        var result = _unit.PrintFromSiWithUnitsInRelativeTime(60);

        Assert.EndsWith($" {item.Symbol}", result, StringComparison.Ordinal);
    }
}
