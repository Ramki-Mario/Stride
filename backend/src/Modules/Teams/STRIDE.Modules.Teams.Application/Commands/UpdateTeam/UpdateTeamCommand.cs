using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Teams.Application.Commands.UpdateTeam;

public sealed record UpdateTeamCommand(
    Guid    TenantId,
    Guid    TeamId,
    string  Name,
    string? Description,
    Guid?   ParentTeamId,
    Guid    UpdatedBy) : IRequest<Result>;
