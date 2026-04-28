using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class TimeSpanUnitExtensions
{
    public static string PrintFromSiWithUnitsInRelativeTime(this TimeSpanUnit unit, double value)
    {
        var timeSpanValue = TimeSpan.FromSeconds(value);
        var abs = timeSpanValue.Duration();

        if (abs < TimeSpan.FromSeconds(1))
        {
            return unit.AvailableUnits[TimeSpanMillisecondUnitItem.Id].PrintFromSiWithUnits(value);
        }

        if (abs < TimeSpan.FromMinutes(1))
        {
            return unit.AvailableUnits[TimeSpanSecondUnitItem.Id].PrintFromSiWithUnits(value, "F0");
        }

        if (abs < TimeSpan.FromHours(1))
        {
            return unit.AvailableUnits[TimeSpanMinuteSecondUnitItem.Id]
                .PrintFromSiWithUnits(value, "F0");
        }

        return unit.AvailableUnits[TimeSpanHourMinuteSecondUnitItem.Id]
            .PrintFromSiWithUnits(value, "F0");
    }
}
