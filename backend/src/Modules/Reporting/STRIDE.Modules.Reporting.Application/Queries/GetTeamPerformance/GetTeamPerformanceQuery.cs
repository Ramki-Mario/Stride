using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetTeamPerformance;

public sealed record GetTeamPerformanceQuery(
    Guid     TenantId,
    DateTime FromDate,
    DateTime ToDate,
    Guid?    RoleId = null)
    : IRequest<Result<IReadOnlyList<TeamMemberPerformanceDto>>>;
