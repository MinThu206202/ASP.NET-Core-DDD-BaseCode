using FluentValidation;
using UserApp.Web.Controllers.Api;

namespace UserApp.Web.Validators.Api;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}
