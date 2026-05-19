using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Reporting.Application.Queries.ExportReportCsv;

/// <summary>
/// Re-runs the underlying query for the saved report and returns the data as UTF-8 CSV bytes.
/// The report type is looked up from the persisted <see cref="Domain.Entities.Report"/> record.
/// </summary>
public sealed record ExportReportCsvQuery(Guid TenantId, Guid ReportId)
    : IRequest<Result<ExportReportCsvResult>>;

/// <summary>CSV payload returned by <see cref="ExportReportCsvQuery"/>.</summary>
/// <param name="FileName">Suggested file name for the download (e.g. "kpi-snapshot-2026-05-19.csv").</param>
/// <param name="Content">UTF-8 encoded CSV bytes.</param>
public sealed record ExportReportCsvResult(string FileName, byte[] Content);
