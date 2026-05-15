using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowDefinitions;

/// <summary>
/// Returns all non-deleted workflow definitions in the current tenant,
/// ordered by most recently updated first.
/// </summary>
public sealed record ListWorkflowDefinitionsQuery : IRequest<Result<IReadOnlyList<WorkflowDefinitionSummaryDto>>>;
