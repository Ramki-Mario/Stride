using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetDashboardKpis;
using STRIDE.Modules.Reporting.Application.Queries.GetWorkflowTrends;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.API.Controllers;

/// <summary>
/// Dashboard read endpoints — KPI snapshot and trend time series.
///
///   GET /api/reporting/dashboard/kpis        — aggregated KPI counts for the header row
///   GET /api/reporting/dashboard/trends      — daily workflow activity trend (last N days)
///   GET /api/reporting/dashboard/trends?days=90
/// </summary>
[ApiController]
[Authorize]
[Route("api/reporting/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator       _mediator;
    private readonly ITenantContext  _tenantContext;

    public DashboardController(IMediator mediator, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns the aggregated KPI snapshot for the dashboard header row.
    /// Counts: total/active definitions, running/completed/failed/cancelled instances.
    /// GET /api/reporting/dashboard/kpis
    /// </summary>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(DashboardKpiDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpis(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetDashboardKpisQuery(_tenantContext.TenantId), ct);

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the daily workflow activity trend for the last <paramref name="days"/> days.
    /// Each data point: date, started count, completed count, failed count.
    /// GET /api/reporting/dashboard/trends?days=30
    /// </summary>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowTrendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrends(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetWorkflowTrendsQuery(_tenantContext.TenantId, days), ct);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
