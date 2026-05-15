using FluentValidation;

namespace STRIDE.Modules.Identity.Application.Queries.GetUser;

public sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
                .WithMessage("UserId is required.");
    }
}
