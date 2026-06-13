using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetRevenueAnalytics;

internal sealed class GetRevenueAnalyticsQueryHandler
    : IRequestHandler<GetRevenueAnalyticsQuery, Result<RevenueAnalyticsDto>>
{
    private readonly IAnalyticsReadService                       _readService;
    private readonly ILogger<GetRevenueAnalyticsQueryHandler>    _logger;

    public GetRevenueAnalyticsQueryHandler(
        IAnalyticsReadService readService,
        ILogger<GetRevenueAnalyticsQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<RevenueAnalyticsDto>> Handle(
        GetRevenueAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.GetRevenueAnalytics(request.TenantId, request.FromDate, request.ToDate);

        var (monthly, byWorkflowType, byClient) = await _readService.GetRevenueAsync(
            request.TenantId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var totalInvoiced = monthly.Sum(m => m.InvoicedAmount);
        var totalPaid     = monthly.Sum(m => m.PaidAmount);

        var dto = new RevenueAnalyticsDto(
            Summary:        new RevenueSummaryDto(totalInvoiced, totalPaid, totalInvoiced - totalPaid),
            MonthlyTrend:   monthly,
            ByWorkflowType: byWorkflowType,
            ByClient:       byClient);

        return Result.Success(dto);
    }
}
