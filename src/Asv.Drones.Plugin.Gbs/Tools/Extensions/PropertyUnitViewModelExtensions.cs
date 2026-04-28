using Asv.Avalonia;
using Asv.Common;
using R3;

namespace Asv.Drones.Plugin.Gbs;

public static class PropertyUnitViewModelExtensions
{
    public static IDisposable EnableUnitValidationRoutable(
        this PropertyUnitViewModel property,
        DialogViewModelBase owner,
        bool isForceValidation = true
    )
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(owner);

        return property.Text.EnableValidationRoutable(
            value => ValidateUnitValue(property, value),
            owner,
            isForceValidation
        );
    }

    public static IDisposable EnableMinUnitValidationRoutable(
        this PropertyUnitViewModel property,
        DialogViewModelBase owner,
        double min,
        bool isForceValidation = true
    )
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(owner);

        return property.Text.EnableValidationRoutable(
            value => ValidateMinUnitValue(property, value, min),
            owner,
            isForceValidation
        );
    }

    private static ValidationResult ValidateUnitValue(PropertyUnitViewModel property, string? value)
    {
        var unitItem = property.Unit.CurrentUnitItem.CurrentValue;
        var unitValidation = unitItem.ValidateValue(value);
        if (unitValidation.IsFailed)
        {
            return unitValidation;
        }

        var siValue = unitItem.ParseToSi(value);
        return double.IsFinite(siValue)
            ? ValidationResult.Success
            : ValidationResult.FailAsNotNumber;
    }

    private static ValidationResult ValidateMinUnitValue(
        PropertyUnitViewModel property,
        string? value,
        double min
    )
    {
        var unitValidation = ValidateUnitValue(property, value);
        if (unitValidation.IsFailed)
        {
            return unitValidation;
        }

        var unitItem = property.Unit.CurrentUnitItem.CurrentValue;
        var siValue = unitItem.ParseToSi(value);
        return siValue >= min
            ? ValidationResult.Success
            : ValidationResult.FailFromErrorMessage(
                $"Value must be at least {unitItem.PrintFromSi(min)} {unitItem.Symbol}"
            );
    }
}
