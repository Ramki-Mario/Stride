using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetDashboardAlerts;

/// <summary>Returns the four actionable alert panels for the dashboard.</summary>
public sealed record GetDashboardAlertsQuery(Guid TenantId)
    : IRequest<Result<DashboardAlertSummaryDto>>;
