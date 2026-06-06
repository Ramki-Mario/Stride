using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Reporting.API.Dtos;
using STRIDE.Modules.Reporting.Application.Commands.GenerateReport;
using STRIDE.Modules.Reporting.Application.Queries.ExportReportCsv;
using STRIDE.Modules.Reporting.Application.Queries.GetReportList;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.API.Controllers;

/// <summary>
/// Report management endpoints.
///
///   GET  /api/reporting/reports                    — list all saved reports, newest first
///   POST /api/reporting/reports/generate           — run a report query and save the audit record
///   GET  /api/reporting/reports/{id}/export        — re-run and download the report as CSV
/// </summary>
[ApiController]
[Authorize]
[Route("api/reporting/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public ReportsController(
        IMediator mediator,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns the list of all saved reports for the current tenant, newest first.
    /// GET /api/reporting/reports
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReports(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetReportListQuery(_tenantContext.TenantId), cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Generates a report snapshot, persists the audit record, and returns the metadata.
    /// The caller can use the returned <c>reportId</c> to export via the /export endpoint.
    /// POST /api/reporting/reports/generate
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateReportResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateReport(
        [FromBody] GenerateReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateReportCommand(
                TenantId:    _tenantContext.TenantId,
                RequestedBy: _currentUser.UserId,
                ReportType:  request.ReportType,
                TrendDays:   request.TrendDays),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Re-runs the underlying query for a saved report and returns a CSV download.
    /// Response: Content-Disposition: attachment; filename="kpi-snapshot-2026-05-19.csv"
    /// GET /api/reporting/reports/{id}/export
    /// </summary>
    [HttpGet("{id:guid}/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportCsv(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExportReportCsvQuery(_tenantContext.TenantId, id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return File(
            result.Value.Content,
            "text/csv; charset=utf-8",
            result.Value.FileName);
    }
}
