using FluentValidation;

namespace STRIDE.Modules.Identity.Application.Commands.RevokeToken;

internal sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
