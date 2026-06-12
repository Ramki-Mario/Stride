using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetDashboardAlerts;

/// <summary>
/// Delegates to <see cref="IReportingReadService"/> to fetch all four alert panels.
/// Pure read — no write side effects.
/// </summary>
internal sealed class GetDashboardAlertsQueryHandler
    : IRequestHandler<GetDashboardAlertsQuery, Result<DashboardAlertSummaryDto>>
{
    private readonly IReportingReadService              _readService;
    private readonly ILogger<GetDashboardAlertsQueryHandler> _logger;

    public GetDashboardAlertsQueryHandler(
        IReportingReadService readService,
        ILogger<GetDashboardAlertsQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<DashboardAlertSummaryDto>> Handle(
        GetDashboardAlertsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.GetDashboardAlerts(request.TenantId);

        var alerts = await _readService.GetDashboardAlertsAsync(request.TenantId, cancellationToken);
        return Result.Success(alerts);
    }
}
