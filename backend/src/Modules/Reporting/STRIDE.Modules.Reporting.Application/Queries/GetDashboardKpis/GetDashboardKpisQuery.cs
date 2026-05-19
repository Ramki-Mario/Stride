using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetDashboardKpis;

/// <summary>Returns the aggregated KPI snapshot for the dashboard header row.</summary>
public sealed record GetDashboardKpisQuery(Guid TenantId)
    : IRequest<Result<DashboardKpiDto>>;
