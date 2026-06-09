using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Application.DTOs;

namespace STRIDE.Modules.Teams.Application.Queries.GetTeamById;

public sealed record GetTeamByIdQuery(
    Guid TenantId,
    Guid TeamId) : IRequest<Result<TeamDetailDto>>;
