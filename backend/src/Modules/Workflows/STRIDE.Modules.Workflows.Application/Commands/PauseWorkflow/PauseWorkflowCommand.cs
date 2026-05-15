using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.PauseWorkflow;

/// <summary>Pauses a Running workflow instance.</summary>
public sealed record PauseWorkflowCommand(
    Guid WorkflowInstanceId,
    Guid PausedBy) : IRequest<Result>;
