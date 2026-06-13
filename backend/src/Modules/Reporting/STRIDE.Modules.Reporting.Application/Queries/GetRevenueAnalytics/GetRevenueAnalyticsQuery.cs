using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetRevenueAnalytics;

public sealed record GetRevenueAnalyticsQuery(
    Guid     TenantId,
    DateTime FromDate,
    DateTime ToDate)
    : IRequest<Result<RevenueAnalyticsDto>>;
