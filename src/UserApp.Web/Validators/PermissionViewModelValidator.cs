using FluentValidation;
using UserApp.Web.ViewModels.Permissions;

namespace UserApp.Web.Validators;

public class PermissionViewModelValidator : AbstractValidator<PermissionViewModel>
{
    public PermissionViewModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Permission name is required")
            .MaximumLength(100).WithMessage("Permission name must be at most 100 characters");
    }
}
