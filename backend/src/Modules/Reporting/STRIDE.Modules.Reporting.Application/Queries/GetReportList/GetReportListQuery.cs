using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetReportList;

/// <summary>Returns the list of all saved reports for the current tenant, newest first.</summary>
public sealed record GetReportListQuery(Guid TenantId)
    : IRequest<Result<IReadOnlyList<ReportSummaryDto>>>;
