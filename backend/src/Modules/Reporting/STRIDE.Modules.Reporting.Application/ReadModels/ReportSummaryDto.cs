namespace STRIDE.Modules.Reporting.Application.ReadModels;

/// <summary>
/// Lightweight summary of a generated report — for list views.
/// </summary>
public sealed record ReportSummaryDto(
    Guid Id,
    string Name,
    string ReportType,
    DateTime GeneratedAt,
    int RecordCount);
