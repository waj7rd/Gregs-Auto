using System.ComponentModel.DataAnnotations;

namespace Gregs_Auto.ViewModels.Validation;

// Model years run slightly ahead of the calendar — a 2027 model can roll in
// during 2026 — so the ceiling is next year, not a hard-coded constant.
public class VehicleYearAttribute : ValidationAttribute
{
    private const int EarliestYear = 1900;

    public override bool IsValid(object? value)
    {
        if (value is not short year)
            return false;

        return year >= EarliestYear && year <= DateTime.Now.Year + 1;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between {EarliestYear} and {DateTime.Now.Year + 1}.";
    }
}
