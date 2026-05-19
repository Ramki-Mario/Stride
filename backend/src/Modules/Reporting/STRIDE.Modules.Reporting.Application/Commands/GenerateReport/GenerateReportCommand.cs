using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Application.Commands.GenerateReport;

/// <summary>
/// Runs a report query for the tenant, persists an audit <see cref="Report"/> record,
/// and returns the metadata. The raw data can be exported afterwards via ExportReportCsv.
/// </summary>
/// <param name="TenantId">Resolved from <c>ITenantContext</c> in the controller.</param>
/// <param name="RequestedBy">UserId of the authenticated user requesting the report.</param>
/// <param name="ReportType">The kind of report to generate.</param>
/// <param name="TrendDays">
/// Only relevant for <see cref="ReportType.WorkflowTrend"/> — lookback window in days (1–365).
/// </param>
public sealed record GenerateReportCommand(
    Guid TenantId,
    Guid RequestedBy,
    ReportType ReportType,
    int TrendDays = 30)
    : IRequest<Result<GenerateReportResult>>;
