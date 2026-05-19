using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Application.Commands.GenerateReport;

/// <summary>
/// Executes the appropriate read query for the requested <see cref="ReportType"/>,
/// persists a <see cref="Report"/> audit record, and returns the metadata.
/// </summary>
internal sealed class GenerateReportCommandHandler
    : IRequestHandler<GenerateReportCommand, Result<GenerateReportResult>>
{
    private readonly IReportingReadService _readService;
    private readonly IReportRepository _reports;
    private readonly ILogger<GenerateReportCommandHandler> _logger;

    public GenerateReportCommandHandler(
        IReportingReadService readService,
        IReportRepository reports,
        ILogger<GenerateReportCommandHandler> logger)
    {
        _readService = readService;
        _reports     = reports;
        _logger      = logger;
    }

    public async Task<Result<GenerateReportResult>> Handle(
        GenerateReportCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "GenerateReport: generating {ReportType} for tenant {TenantId} by user {UserId}",
            request.ReportType, request.TenantId, request.RequestedBy);

        // ── Strategy: compute record count per report type ─────────────────
        var (reportName, recordCount) = request.ReportType switch
        {
            ReportType.DashboardKpi => await GenerateDashboardKpiAsync(request, ct),
            ReportType.WorkflowTrend => await GenerateWorkflowTrendAsync(request, ct),
            ReportType.WorkflowSummary => await GenerateWorkflowSummaryAsync(request, ct),
            _ => throw new InvalidOperationException($"Unsupported report type: {request.ReportType}")
        };

        // ── Persist audit record ────────────────────────────────────────────
        var report = Report.Create(
            tenantId:    request.TenantId,
            createdBy:   request.RequestedBy,
            name:        reportName,
            reportType:  request.ReportType,
            recordCount: recordCount);

        await _reports.AddAsync(report, ct);
        await _reports.SaveChangesAsync(ct);

        _logger.LogInformation(
            "GenerateReport: report {ReportId} ({Name}) saved with {Count} records",
            report.Id, report.Name, report.RecordCount);

        return Result.Success(new GenerateReportResult(
            report.Id,
            report.Name,
            report.ReportType,
            report.RecordCount,
            report.GeneratedAt));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<(string Name, int RecordCount)> GenerateDashboardKpiAsync(
        GenerateReportCommand request, CancellationToken ct)
    {
        // KPI is always a single row of aggregate counts.
        await _readService.GetDashboardKpisAsync(request.TenantId, ct);
        var name = $"KPI Snapshot — {DateTime.UtcNow:yyyy-MM-dd}";
        return (name, 1);
    }

    private async Task<(string Name, int RecordCount)> GenerateWorkflowTrendAsync(
        GenerateReportCommand request, CancellationToken ct)
    {
        var trends = await _readService.GetWorkflowTrendsAsync(request.TenantId, request.TrendDays, ct);
        var name = $"Workflow Trend ({request.TrendDays}d) — {DateTime.UtcNow:yyyy-MM-dd}";
        return (name, trends.Count);
    }

    private async Task<(string Name, int RecordCount)> GenerateWorkflowSummaryAsync(
        GenerateReportCommand request, CancellationToken ct)
    {
        var rows = await _readService.GetWorkflowSummaryAsync(request.TenantId, ct);
        var name = $"Workflow Summary — {DateTime.UtcNow:yyyy-MM-dd}";
        return (name, rows.Count);
    }
}
