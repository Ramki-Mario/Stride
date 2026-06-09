using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Domain.Exceptions;
using STRIDE.Modules.Teams.Domain.Repositories;

namespace STRIDE.Modules.Teams.Application.Commands.UpdateTeam;

internal sealed class UpdateTeamCommandHandler
    : IRequestHandler<UpdateTeamCommand, Result>
{
    private readonly ITeamRepository _teams;
    private readonly IAuditLogger    _audit;
    private readonly ICurrentUser    _currentUser;

    public UpdateTeamCommandHandler(
        ITeamRepository teams,
        IAuditLogger    audit,
        ICurrentUser    currentUser)
    {
        _teams       = teams;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teams.GetByIdAsync(request.TenantId, request.TeamId, cancellationToken);
        if (team is null)
            return Result.Failure($"Team '{request.TeamId}' not found.");

        if (await _teams.ExistsByNameAsync(
                request.TenantId, request.Name, excludeId: request.TeamId, cancellationToken))
            return Result.Failure($"A team named '{request.Name}' already exists.");

        try
        {
            team.Update(request.Name, request.Description, request.ParentTeamId, request.UpdatedBy);
        }
        catch (TeamDomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _teams.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.UpdatedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.TeamUpdated,
            ResourceType: "Team",
            ResourceId:   team.Id,
            NewValueJson: $"{{\"name\":\"{team.Name}\"}}"));

        return Result.Success();
    }
}
