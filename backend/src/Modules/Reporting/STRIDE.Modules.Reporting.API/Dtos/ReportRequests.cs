using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.API.Dtos;

/// <summary>
/// Request body for POST /api/reporting/reports/generate.
/// </summary>
/// <param name="ReportType">The kind of report to generate (DashboardKpi, WorkflowTrend, WorkflowSummary).</param>
/// <param name="TrendDays">
/// Lookback window in days for <see cref="ReportType.WorkflowTrend"/> reports.
/// Must be between 1 and 365. Ignored for other report types.
/// </param>
public sealed record GenerateReportRequest(
    ReportType ReportType,
    int TrendDays = 30);
