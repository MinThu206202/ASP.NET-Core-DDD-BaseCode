using FluentValidation;
using UserApp.Web.Controllers.Api;

namespace UserApp.Web.Validators.Api;

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required")
            .Length(6).WithMessage("OTP must be 6 digits")
            .Matches("^[0-9]{6}$").WithMessage("OTP must be numeric");
    }
}
