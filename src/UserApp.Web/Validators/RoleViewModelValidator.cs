using FluentValidation;
using UserApp.Web.ViewModels.Roles;

namespace UserApp.Web.Validators;

public class RoleViewModelValidator : AbstractValidator<RoleViewModel>
{
    public RoleViewModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .Length(2, 50).WithMessage("Role name must be between 2 and 50 characters");
    }
}
