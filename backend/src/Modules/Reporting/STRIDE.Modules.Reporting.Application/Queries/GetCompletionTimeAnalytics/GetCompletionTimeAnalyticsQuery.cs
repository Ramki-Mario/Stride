using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetCompletionTimeAnalytics;

/// <summary>
/// Returns all three completion-time analytics datasets for the given tenant and date range.
/// <paramref name="WorkflowDefinitionId"/> is optional — omit to aggregate across all definitions.
/// </summary>
public sealed record GetCompletionTimeAnalyticsQuery(
    Guid      TenantId,
    DateTime  FromDate,
    DateTime  ToDate,
    Guid?     WorkflowDefinitionId = null)
    : IRequest<Result<CompletionTimeAnalyticsDto>>;
