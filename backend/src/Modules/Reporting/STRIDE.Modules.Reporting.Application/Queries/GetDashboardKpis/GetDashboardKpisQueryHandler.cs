using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetDashboardKpis;

/// <summary>
/// Delegates to <see cref="IReportingReadService"/> to fetch the aggregated KPI snapshot.
/// Pure read — no write side effects.
/// </summary>
internal sealed class GetDashboardKpisQueryHandler
    : IRequestHandler<GetDashboardKpisQuery, Result<DashboardKpiDto>>
{
    private readonly IReportingReadService _readService;
    private readonly ILogger<GetDashboardKpisQueryHandler> _logger;

    public GetDashboardKpisQueryHandler(
        IReportingReadService readService,
        ILogger<GetDashboardKpisQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<DashboardKpiDto>> Handle(
        GetDashboardKpisQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "GetDashboardKpis: fetching KPI snapshot for tenant {TenantId}", request.TenantId);

        var kpis = await _readService.GetDashboardKpisAsync(request.TenantId, ct);
        return Result.Success(kpis);
    }
}
