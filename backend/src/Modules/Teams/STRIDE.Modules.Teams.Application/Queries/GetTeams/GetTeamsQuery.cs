using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Application.DTOs;

namespace STRIDE.Modules.Teams.Application.Queries.GetTeams;

public sealed record GetTeamsQuery(
    Guid    TenantId,
    string? Search,
    int?    Status,
    int     Page     = 1,
    int     PageSize = 20) : IRequest<Result<PagedResult<TeamSummaryDto>>>;
