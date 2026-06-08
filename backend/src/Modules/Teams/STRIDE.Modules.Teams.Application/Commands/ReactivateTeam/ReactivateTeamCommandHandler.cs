using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Domain.Exceptions;
using STRIDE.Modules.Teams.Domain.Repositories;

namespace STRIDE.Modules.Teams.Application.Commands.ReactivateTeam;

internal sealed class ReactivateTeamCommandHandler
    : IRequestHandler<ReactivateTeamCommand, Result>
{
    private readonly ITeamRepository _teams;
    private readonly IAuditLogger    _audit;
    private readonly ICurrentUser    _currentUser;

    public ReactivateTeamCommandHandler(
        ITeamRepository teams,
        IAuditLogger    audit,
        ICurrentUser    currentUser)
    {
        _teams       = teams;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        ReactivateTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teams.GetByIdAsync(request.TenantId, request.TeamId, cancellationToken);
        if (team is null)
            return Result.Failure($"Team '{request.TeamId}' not found.");

        try
        {
            team.Reactivate(request.ReactivatedBy);
        }
        catch (TeamDomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _teams.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.ReactivatedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.TeamReactivated,
            ResourceType: "Team",
            ResourceId:   team.Id));

        return Result.Success();
    }
}
