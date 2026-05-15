using FluentValidation;

namespace STRIDE.Modules.Identity.Application.Commands.RegisterUser;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
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
            .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters.")
            .MaximumLength(256)
                .WithMessage("Password must not exceed 256 characters.")
            .Matches(@"[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]")
                .WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z0-9]")
                .WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
                .WithMessage("Display name is required.")
            .MinimumLength(2)
                .WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(100)
                .WithMessage("Display name must not exceed 100 characters.");
    }
}
