using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetCompletionTimeAnalytics;

internal sealed class GetCompletionTimeAnalyticsQueryHandler
    : IRequestHandler<GetCompletionTimeAnalyticsQuery, Result<CompletionTimeAnalyticsDto>>
{
    private readonly IAnalyticsReadService                           _readService;
    private readonly ILogger<GetCompletionTimeAnalyticsQueryHandler> _logger;

    public GetCompletionTimeAnalyticsQueryHandler(
        IAnalyticsReadService readService,
        ILogger<GetCompletionTimeAnalyticsQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<CompletionTimeAnalyticsDto>> Handle(
        GetCompletionTimeAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.GetCompletionTimeAnalytics(request.TenantId, request.FromDate, request.ToDate);

        var data = await _readService.GetCompletionTimesAsync(
            request.TenantId,
            request.FromDate,
            request.ToDate,
            request.WorkflowDefinitionId,
            cancellationToken);

        return Result.Success(data);
    }
}
