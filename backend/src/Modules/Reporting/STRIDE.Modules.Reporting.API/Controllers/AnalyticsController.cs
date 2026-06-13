using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetCompletionTimeAnalytics;
using STRIDE.Modules.Reporting.Application.Queries.GetRevenueAnalytics;
using STRIDE.Modules.Reporting.Application.Queries.GetTeamPerformance;
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

    /// <summary>
    /// Returns team performance metrics — one row per user with at least one
    /// completed step assigned in the date range.
    /// GET /api/analytics/team/performance?fromDate=YYYY-MM-DD&amp;toDate=YYYY-MM-DD[&amp;roleId=GUID]
    /// </summary>
    [HttpGet("team/performance")]
    [ProducesResponseType(typeof(IReadOnlyList<TeamMemberPerformanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTeamPerformance(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid?    roleId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTeamPerformanceQuery(
                _tenantContext.TenantId,
                fromDate.ToUniversalTime(),
                toDate.ToUniversalTime(),
                roleId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns a CSV file of team performance for the given date range.
    /// GET /api/analytics/team/performance/export?fromDate=YYYY-MM-DD&amp;toDate=YYYY-MM-DD
    /// </summary>
    [HttpGet("team/performance/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportTeamPerformance(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid?    roleId = null,
        CancellationToken cancellationToken = default)
    {
        if (toDate <= fromDate)
            return BadRequest(new { error = "ToDate must be after FromDate." });

        var rows = await _analyticsReadService.GetTeamPerformanceAsync(
            _tenantContext.TenantId,
            fromDate.ToUniversalTime(),
            toDate.ToUniversalTime(),
            roleId,
            cancellationToken);

        var csv      = BuildTeamPerformanceCsv(rows);
        var fileName = $"team-performance-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";

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

    /// <summary>
    /// Returns revenue analytics: summary KPIs, monthly trend, by-workflow-type breakdown,
    /// and per-client ranking — filtered to workflow-linked Sent/Paid invoices.
    /// GET /api/analytics/revenue?fromDate=YYYY-MM-DD&amp;toDate=YYYY-MM-DD
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRevenueAnalytics(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetRevenueAnalyticsQuery(
                _tenantContext.TenantId,
                fromDate.ToUniversalTime(),
                toDate.ToUniversalTime()),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns a CSV file of individual invoices for the revenue date range.
    /// GET /api/analytics/revenue/export?fromDate=YYYY-MM-DD&amp;toDate=YYYY-MM-DD
    /// </summary>
    [HttpGet("revenue/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportRevenue(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (toDate <= fromDate)
            return BadRequest(new { error = "ToDate must be after FromDate." });

        var rows = await _analyticsReadService.GetRevenueExportAsync(
            _tenantContext.TenantId,
            fromDate.ToUniversalTime(),
            toDate.ToUniversalTime(),
            cancellationToken);

        var csv      = BuildRevenueCsv(rows);
        var fileName = $"revenue-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    private static string BuildTeamPerformanceCsv(IReadOnlyList<TeamMemberPerformanceDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DisplayName,Email,Role,CompletedSteps,CompletedWorkflows,AvgStepDurationMinutes,OverdueRate");

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                $"\"{r.DisplayName.Replace("\"", "\"\"")}\"",
                $"\"{r.Email.Replace("\"", "\"\"")}\"",
                $"\"{r.Role.Replace("\"", "\"\"")}\"",
                r.CompletedSteps,
                r.CompletedWorkflows,
                r.AvgStepDurationMinutes.ToString("F1", CultureInfo.InvariantCulture),
                r.OverdueRate.ToString("F1", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    private static string BuildRevenueCsv(IReadOnlyList<RevenueExportRowDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("InvoiceNumber,ClientName,WorkflowType,Currency,TotalAmount,Status,SentAt");

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                $"\"{r.InvoiceNumber.Replace("\"", "\"\"")}\"",
                $"\"{r.ClientName.Replace("\"", "\"\"")}\"",
                $"\"{r.WorkflowType.Replace("\"", "\"\"")}\"",
                r.Currency,
                r.TotalAmount.ToString("F2", CultureInfo.InvariantCulture),
                r.Status,
                r.SentAt.ToString("o", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }
}
