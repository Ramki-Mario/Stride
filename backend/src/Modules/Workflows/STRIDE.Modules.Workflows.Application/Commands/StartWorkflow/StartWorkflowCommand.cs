using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.StartWorkflow;

/// <summary>
/// Instantiates a workflow from an Active definition, snapshotting its steps
/// and transitioning the new instance to Running status.
/// </summary>
public sealed record StartWorkflowCommand(
    Guid  WorkflowDefinitionId,
    Guid  StartedBy,
    Guid? ClientId = null) : IRequest<Result<StartWorkflowResult>>;
