using FluentValidation;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Validators;

public class EditUserViewModelValidator : AbstractValidator<EditUserViewModel>
{
    public EditUserViewModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must be at most 200 characters");
    }
}
