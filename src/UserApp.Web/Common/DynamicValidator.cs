using System.ComponentModel.DataAnnotations;

namespace UserApp.Web.Common;

public static class DynamicValidator
{
    public static List<ValidationResult> Validate<T>(T model)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            model!,
            new ValidationContext(model!),
            results,
            true);

        return results;
    }
}