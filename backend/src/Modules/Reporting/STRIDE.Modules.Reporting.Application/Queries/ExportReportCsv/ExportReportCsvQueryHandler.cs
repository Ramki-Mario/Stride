using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;
using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Application.Queries.ExportReportCsv;

/// <summary>
/// Looks up the saved report to determine its type, re-runs the underlying read query,
/// and renders the result as UTF-8 CSV using a simple <see cref="StringBuilder"/> approach —
/// no external CSV library required for flat projections.
/// </summary>
internal sealed class ExportReportCsvQueryHandler
    : IRequestHandler<ExportReportCsvQuery, Result<ExportReportCsvResult>>
{
    private readonly IReportRepository _reports;
    private readonly IReportingReadService _readService;
    private readonly ILogger<ExportReportCsvQueryHandler> _logger;

    public ExportReportCsvQueryHandler(
        IReportRepository reports,
        IReportingReadService readService,
        ILogger<ExportReportCsvQueryHandler> logger)
    {
        _reports     = reports;
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<ExportReportCsvResult>> Handle(
        ExportReportCsvQuery request,
        CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(request.ReportId, ct);
        if (report is null)
        {
            _logger.LogWarning(
                "ExportReportCsv: report {ReportId} not found for tenant {TenantId}",
                request.ReportId, request.TenantId);
            return Result.Failure<ExportReportCsvResult>(
                $"Report '{request.ReportId}' not found.");
        }

        _logger.LogInformation(
            "ExportReportCsv: exporting {ReportType} report {ReportId} for tenant {TenantId}",
            report.ReportType, report.Id, request.TenantId);

        var csv = report.ReportType switch
        {
            ReportType.DashboardKpi =>
                await BuildDashboardKpiCsvAsync(request.TenantId, ct),
            ReportType.WorkflowTrend =>
                await BuildWorkflowTrendCsvAsync(request.TenantId, ct),
            ReportType.WorkflowSummary =>
                await BuildWorkflowSummaryCsvAsync(request.TenantId, ct),
            _ => throw new InvalidOperationException($"No CSV exporter for report type: {report.ReportType}")
        };

        var fileName = BuildFileName(report);
        var bytes    = Encoding.UTF8.GetBytes(csv);

        return Result.Success(new ExportReportCsvResult(fileName, bytes));
    }

    // ── CSV builders ──────────────────────────────────────────────────────────

    private async Task<string> BuildDashboardKpiCsvAsync(Guid tenantId, CancellationToken ct)
    {
        var kpi = await _readService.GetDashboardKpisAsync(tenantId, ct);
        var sb  = new StringBuilder();

        sb.AppendLine("TotalDefinitions,ActiveDefinitions,RunningInstances,CompletedInstances,FailedInstances,CancelledInstances");
        sb.AppendLine($"{kpi.TotalDefinitions},{kpi.ActiveDefinitions},{kpi.RunningInstances},{kpi.CompletedInstances},{kpi.FailedInstances},{kpi.CancelledInstances}");

        return sb.ToString();
    }

    private async Task<string> BuildWorkflowTrendCsvAsync(Guid tenantId, CancellationToken ct)
    {
        var trends = await _readService.GetWorkflowTrendsAsync(tenantId, ct: ct);
        var sb     = new StringBuilder();

        sb.AppendLine("TrendDate,Started,Completed,Failed");
        foreach (var row in trends)
            sb.AppendLine($"{row.TrendDate:yyyy-MM-dd},{row.Started},{row.Completed},{row.Failed}");

        return sb.ToString();
    }

    private async Task<string> BuildWorkflowSummaryCsvAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await _readService.GetWorkflowSummaryAsync(tenantId, ct);
        var sb   = new StringBuilder();

        sb.AppendLine("WorkflowName,Status,TotalInstances,RunningInstances,CompletedInstances,FailedInstances");
        foreach (var row in rows)
            sb.AppendLine($"{EscapeCsv(row.WorkflowName)},{row.Status},{row.TotalInstances},{row.RunningInstances},{row.CompletedInstances},{row.FailedInstances}");

        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildFileName(Report report)
    {
        var slug = report.ReportType switch
        {
            ReportType.DashboardKpi    => "kpi-snapshot",
            ReportType.WorkflowTrend   => "workflow-trend",
            ReportType.WorkflowSummary => "workflow-summary",
            _                          => "report"
        };
        return $"{slug}-{report.GeneratedAt:yyyy-MM-dd}.csv";
    }

    /// <summary>
    /// Minimal CSV escaping: wraps a field in double-quotes if it contains
    /// a comma, double-quote, or newline.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
