using Microsoft.Extensions.Logging;
using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Application;

internal static partial class Log
{
    // ── GenerateReport ────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "GenerateReport: generating {ReportType} for tenant {TenantId} by user {UserId}")]
    internal static partial void GenerateReportStarted(
        this ILogger logger, ReportType reportType, Guid tenantId, Guid userId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GenerateReport: report {ReportId} ({Name}) saved with {Count} records")]
    internal static partial void GenerateReportSaved(
        this ILogger logger, Guid reportId, string name, int count);

    // ── ExportReportCsv ───────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "ExportReportCsv: report {ReportId} not found for tenant {TenantId}")]
    internal static partial void ExportReportNotFound(this ILogger logger, Guid reportId, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "ExportReportCsv: exporting {ReportType} report {ReportId} for tenant {TenantId}")]
    internal static partial void ExportReportStarted(
        this ILogger logger, ReportType reportType, Guid reportId, Guid tenantId);

    // ── Queries ───────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "GetDashboardKpis: fetching KPI snapshot for tenant {TenantId}")]
    internal static partial void GetDashboardKpis(this ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GetReportList: listing saved reports for tenant {TenantId}")]
    internal static partial void GetReportList(this ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GetWorkflowTrends: fetching {Days}-day trend for tenant {TenantId}")]
    internal static partial void GetWorkflowTrends(this ILogger logger, int days, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GetDashboardAlerts: fetching alert panels for tenant {TenantId}")]
    internal static partial void GetDashboardAlerts(this ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GetTeamWorkload: fetching team workload for tenant {TenantId}")]
    internal static partial void GetTeamWorkload(this ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "GetCompletionTimeAnalytics: fetching completion times for tenant {TenantId} ({FromDate} – {ToDate})")]
    internal static partial void GetCompletionTimeAnalytics(
        this ILogger logger, Guid tenantId, DateTime fromDate, DateTime toDate);
}
