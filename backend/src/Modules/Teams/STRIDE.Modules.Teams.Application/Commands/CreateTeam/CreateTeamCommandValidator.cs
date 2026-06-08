using FluentValidation;
using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Application.Commands.CreateTeam;

internal sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Team.NameMaxLength);
        RuleFor(x => x.Description)
            .MaximumLength(Team.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}
