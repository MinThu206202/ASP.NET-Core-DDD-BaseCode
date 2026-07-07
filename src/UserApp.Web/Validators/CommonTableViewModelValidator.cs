using FluentValidation;
using UserApp.Web.ViewModels.CommonTables;

namespace UserApp.Web.Validators;

public class CommonTableViewModelValidator : AbstractValidator<CommonTableViewModel>
{
    public CommonTableViewModelValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .MaximumLength(100).WithMessage("Type must be at most 100 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(100).WithMessage("Code must be at most 100 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters");
    }
}
