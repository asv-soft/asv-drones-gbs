using Asv.Avalonia;

namespace Asv.Drones.Plugin.Gbs;

public static class TimeSpanUnitExtensions
{
    public static string PrintFromSiWithUnitsInRelativeTime(this TimeSpanUnit unit, double value)
    {
        var unitItem = unit.GetRelativeTimeUnitItem(value);
        return $"{unit.PrintFromSiInRelativeTime(value)} {unitItem.Symbol}";
    }

    public static IUnitItem GetRelativeTimeUnitItem(this TimeSpanUnit unit, double value)
    {
        var timeSpanValue = TimeSpan.FromSeconds(value);
        var abs = timeSpanValue.Duration();

        if (abs < TimeSpan.FromSeconds(1))
        {
            return unit.AvailableUnits[TimeSpanMillisecondUnitItem.Id];
        }

        if (abs < TimeSpan.FromMinutes(1))
        {
            return unit.AvailableUnits[TimeSpanSecondUnitItem.Id];
        }

        if (abs < TimeSpan.FromHours(1))
        {
            return unit.AvailableUnits[TimeSpanMinuteSecondUnitItem.Id];
        }

        return unit.AvailableUnits[TimeSpanHourMinuteSecondUnitItem.Id];
    }

    public static string PrintFromSiInRelativeTime(this TimeSpanUnit unit, double value)
    {
        var unitItem = unit.GetRelativeTimeUnitItem(value);
        return unitItem is TimeSpanMillisecondUnitItem
            ? unitItem.PrintFromSi(value)
            : unitItem.PrintFromSi(value, "F0");
    }
}
