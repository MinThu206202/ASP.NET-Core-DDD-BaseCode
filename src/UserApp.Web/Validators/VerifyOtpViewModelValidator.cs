using FluentValidation;
using UserApp.Web.ViewModels;

namespace UserApp.Web.Validators;

public class VerifyOtpViewModelValidator : AbstractValidator<VerifyOtpViewModel>
{
    public VerifyOtpViewModelValidator()
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
