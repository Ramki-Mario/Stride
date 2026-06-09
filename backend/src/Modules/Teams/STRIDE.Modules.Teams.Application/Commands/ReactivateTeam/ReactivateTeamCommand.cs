using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Teams.Application.Commands.ReactivateTeam;

public sealed record ReactivateTeamCommand(
    Guid TenantId,
    Guid TeamId,
    Guid ReactivatedBy) : IRequest<Result>;
