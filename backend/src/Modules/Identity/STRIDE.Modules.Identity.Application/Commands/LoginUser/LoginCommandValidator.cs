using FluentValidation;

namespace STRIDE.Modules.Identity.Application.Commands.LoginUser;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email must be a valid email address.")
            .MaximumLength(256)
                .WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("Password is required.")
            .MaximumLength(256)
                .WithMessage("Password must not exceed 256 characters.");
    }
}
