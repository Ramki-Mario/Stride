using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.SignOffPublicJob;

internal sealed class SignOffPublicJobCommandValidator : AbstractValidator<SignOffPublicJobCommand>
{
    public SignOffPublicJobCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.ClientName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
