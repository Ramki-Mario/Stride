using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.ActivateWorkflow;

/// <summary>
/// Transitions a workflow definition from Draft to Active, making it
/// available for instantiation. Requires at least one step to be defined.
/// </summary>
public sealed record ActivateWorkflowCommand(
    Guid WorkflowDefinitionId,
    Guid ActivatedBy) : IRequest<Result>;
