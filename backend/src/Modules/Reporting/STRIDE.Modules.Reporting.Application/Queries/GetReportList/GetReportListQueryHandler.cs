using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetReportList;

/// <summary>
/// Delegates to <see cref="IReportingReadService"/> to return the saved-report list.
/// Pure read — no write side effects.
/// </summary>
internal sealed class GetReportListQueryHandler
    : IRequestHandler<GetReportListQuery, Result<IReadOnlyList<ReportSummaryDto>>>
{
    private readonly IReportingReadService _readService;
    private readonly ILogger<GetReportListQueryHandler> _logger;

    public GetReportListQueryHandler(
        IReportingReadService readService,
        ILogger<GetReportListQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<IReadOnlyList<ReportSummaryDto>>> Handle(
        GetReportListQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "GetReportList: listing saved reports for tenant {TenantId}", request.TenantId);

        var reports = await _readService.GetReportSummariesAsync(request.TenantId, ct);
        return Result.Success(reports);
    }
}
