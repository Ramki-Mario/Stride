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
}
