using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetCompletionTimeAnalytics;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.API.Controllers;

/// <summary>
/// Operational analytics endpoints — completion time and bottleneck views.
///
///   GET /api/analytics/completion-times        — three-dataset analytics response
///   GET /api/analytics/completion-times/export — CSV download of individual instances
/// </summary>
[ApiController]
[Authorize]
[Route("api/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IMediator            _mediator;
    private readonly IAnalyticsReadService _analyticsReadService;
    private readonly ITenantContext        _tenantContext;

    public AnalyticsController(
        IMediator             mediator,
        IAnalyticsReadService analyticsReadService,
        ITenantContext        tenantContext)
    {
        _mediator             = mediator;
        _analyticsReadService = analyticsReadService;
        _tenantContext        = tenantContext;
    }

    /// <summary>
    /// Returns active workflow definitions for the analytics filter dropdown.
    /// GET /api/analytics/workflow-definitions
    /// </summary>
    [HttpGet("workflow-definitions")]
    [ProducesResponseType(typeof(IReadOnlyList<AnalyticsWorkflowDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkflowDefinitions(CancellationToken cancellationToken)
    {
        var definitions = await _analyticsReadService.GetWorkflowDefinitionsAsync(
            _tenantContext.TenantId, cancellationToken);
        return Ok(definitions);
    }

    /// <summary>
    /// Returns completion-time analytics: average duration by definition, step bottlenecks,
    /// and a weekly trend series — all filtered by date range and optional definition.
    /// GET /api/analytics/completion-times?fromDate=YYYY-MM-DD&amp;toDate=YYYY-MM-DD[&amp;workflowDefinitionId=GUID]
    /// </summary>
    [HttpGet("completion-times")]
    [ProducesResponseType(typeof(CompletionTimeAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCompletionTimes(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid?    workflowDefinitionId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCompletionTimeAnalyticsQuery(
                _tenantContext.TenantId,
                fromDate.ToUniversalTime(),
                toDate.ToUniversalTime(),
                workflowDefinitionId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns a CSV file of completed workflow instances in the given date range.
    /// GET /api/analytics/completion-times/export?fromDate=YYYY-MM-DD&amp;toDate=YYYY-MM-DD
    /// </summary>
    [HttpGet("completion-times/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCompletionTimes(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid?    workflowDefinitionId = null,
        CancellationToken cancellationToken = default)
    {
        if (toDate <= fromDate)
            return BadRequest(new { error = "ToDate must be after FromDate." });

        var rows = await _analyticsReadService.GetCompletionTimesExportAsync(
            _tenantContext.TenantId,
            fromDate.ToUniversalTime(),
            toDate.ToUniversalTime(),
            workflowDefinitionId,
            cancellationToken);

        var csv = BuildCsv(rows);
        var fileName = $"completion-times-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    private static string BuildCsv(IReadOnlyList<CompletionTimeExportRowDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("InstanceId,WorkflowName,StartedAt,CompletedAt,DurationMinutes");

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                r.InstanceId,
                $"\"{r.WorkflowName.Replace("\"", "\"\"")}\"",
                r.StartedAt.ToString("o", CultureInfo.InvariantCulture),
                r.CompletedAt.ToString("o", CultureInfo.InvariantCulture),
                r.DurationMinutes.ToString("F1", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }
}
