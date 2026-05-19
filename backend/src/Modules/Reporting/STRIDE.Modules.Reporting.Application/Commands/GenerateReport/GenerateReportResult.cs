using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Application.Commands.GenerateReport;

/// <summary>
/// Returned after a report has been successfully generated and persisted.
/// The client can use <see cref="ReportId"/> to export the data via ExportReportCsv.
/// </summary>
public sealed record GenerateReportResult(
    Guid ReportId,
    string Name,
    ReportType ReportType,
    int RecordCount,
    DateTime GeneratedAt);
