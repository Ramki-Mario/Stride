using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetWorkflowTrends;

/// <summary>
/// Returns the daily workflow activity trend series for the last <see cref="Days"/> days.
/// </summary>
public sealed record GetWorkflowTrendsQuery(Guid TenantId, int Days = 30)
    : IRequest<Result<IReadOnlyList<WorkflowTrendDto>>>;
