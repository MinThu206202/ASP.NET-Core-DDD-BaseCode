using FluentValidation;
using UserApp.Web.Controllers.Api;

namespace UserApp.Web.Validators.Api;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");
    }
}
