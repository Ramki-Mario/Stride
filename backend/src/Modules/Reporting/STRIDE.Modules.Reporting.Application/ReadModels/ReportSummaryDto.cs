namespace STRIDE.Modules.Reporting.Application.ReadModels;

/// <summary>
/// Lightweight summary of a generated report — for list views.
/// Column aliases in GetReportSummaries.sql ensure Dapper maps to these property names.
/// </summary>
public sealed record ReportSummaryDto(
    Guid ReportId,
    string Name,
    string ReportType,
    DateTime GeneratedAt,
    int RecordCount,
    Guid RequestedBy);
