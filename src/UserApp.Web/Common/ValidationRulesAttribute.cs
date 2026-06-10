using System.ComponentModel.DataAnnotations;

namespace UserApp.Web.Common;

[AttributeUsage(AttributeTargets.Property)]
public class ValidationRulesAttribute : ValidationAttribute
{
    public int MinLength { get; set; }

    public decimal MinValue { get; set; }

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true;

        if (value is string text)
        {
            if (MinLength > 0 && text.Length < MinLength)
                return false;
        }

        if (decimal.TryParse(value.ToString(), out var number))
        {
            if (MinValue > 0 && number <= MinValue)
                return false;
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        if (MinLength > 0)
            return $"{name} must be at least {MinLength} characters.";

        if (MinValue > 0)
            return $"{name} must be greater than {MinValue}.";

        return base.FormatErrorMessage(name);
    }
}