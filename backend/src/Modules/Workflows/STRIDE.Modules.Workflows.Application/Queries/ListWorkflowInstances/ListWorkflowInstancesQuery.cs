using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowInstances;

/// <summary>
/// Returns all workflow instances in the current tenant, ordered by most recently updated first.
/// Optionally filters by definition ID (run history of a specific template) or team assignment.
/// </summary>
public sealed record ListWorkflowInstancesQuery(
    Guid? WorkflowDefinitionId = null,
    Guid? TeamId = null)
    : IRequest<Result<IReadOnlyList<WorkflowInstanceSummaryDto>>>;
