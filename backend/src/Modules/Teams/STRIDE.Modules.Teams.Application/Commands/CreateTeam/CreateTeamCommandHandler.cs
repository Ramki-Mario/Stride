using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Domain.Entities;
using STRIDE.Modules.Teams.Domain.Exceptions;
using STRIDE.Modules.Teams.Domain.Repositories;

namespace STRIDE.Modules.Teams.Application.Commands.CreateTeam;

internal sealed class CreateTeamCommandHandler
    : IRequestHandler<CreateTeamCommand, Result<Guid>>
{
    private readonly ITeamRepository _teams;
    private readonly IAuditLogger    _audit;
    private readonly ICurrentUser    _currentUser;

    public CreateTeamCommandHandler(
        ITeamRepository teams,
        IAuditLogger    audit,
        ICurrentUser    currentUser)
    {
        _teams       = teams;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CreateTeamCommand request, CancellationToken cancellationToken)
    {
        if (await _teams.ExistsByNameAsync(request.TenantId, request.Name, excludeId: null, cancellationToken))
            return Result.Failure<Guid>($"A team named '{request.Name}' already exists.");

        Team team;
        try
        {
            team = Team.Create(new NewTeam(
                request.TenantId,
                request.Name,
                request.Description,
                request.ParentTeamId,
                request.CreatedBy));
        }
        catch (TeamDomainException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        await _teams.AddAsync(team, cancellationToken);
        await _teams.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.CreatedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.TeamCreated,
            ResourceType: "Team",
            ResourceId:   team.Id,
            NewValueJson: $"{{\"name\":\"{team.Name}\"}}"));

        return Result.Success(team.Id);
    }
}
