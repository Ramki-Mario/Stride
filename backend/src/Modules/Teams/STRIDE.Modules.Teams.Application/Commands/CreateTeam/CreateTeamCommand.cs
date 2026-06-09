using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Teams.Application.Commands.CreateTeam;

public sealed record CreateTeamCommand(
    Guid    TenantId,
    string  Name,
    string? Description,
    Guid?   ParentTeamId,
    Guid    CreatedBy) : IRequest<Result<Guid>>;
