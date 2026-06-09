using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Teams.Application.Commands.DeactivateTeam;

public sealed record DeactivateTeamCommand(
    Guid TenantId,
    Guid TeamId,
    Guid DeactivatedBy) : IRequest<Result>;
