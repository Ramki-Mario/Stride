using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetWorkflowTrends;

/// <summary>
/// Delegates to <see cref="IReportingReadService"/> to fetch the trend time series.
/// Pure read — no write side effects.
/// </summary>
internal sealed class GetWorkflowTrendsQueryHandler
    : IRequestHandler<GetWorkflowTrendsQuery, Result<IReadOnlyList<WorkflowTrendDto>>>
{
    private readonly IReportingReadService _readService;
    private readonly ILogger<GetWorkflowTrendsQueryHandler> _logger;

    public GetWorkflowTrendsQueryHandler(
        IReportingReadService readService,
        ILogger<GetWorkflowTrendsQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<IReadOnlyList<WorkflowTrendDto>>> Handle(
        GetWorkflowTrendsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetWorkflowTrends: fetching {Days}-day trend for tenant {TenantId}",
            request.Days, request.TenantId);

        var trends = await _readService.GetWorkflowTrendsAsync(request.TenantId, request.Days, cancellationToken);
        return Result.Success(trends);
    }
}
