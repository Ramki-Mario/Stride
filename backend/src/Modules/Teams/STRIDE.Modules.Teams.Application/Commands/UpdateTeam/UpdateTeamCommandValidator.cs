using FluentValidation;
using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Application.Commands.UpdateTeam;

internal sealed class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Team.NameMaxLength);
        RuleFor(x => x.Description)
            .MaximumLength(Team.DescriptionMaxLength)
            .When(x => x.Description is not null);
    }
}
