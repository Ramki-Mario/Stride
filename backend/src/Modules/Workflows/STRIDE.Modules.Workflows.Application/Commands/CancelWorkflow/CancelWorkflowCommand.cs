using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.CancelWorkflow;

/// <summary>Cancels a Running or Paused workflow instance.</summary>
public sealed record CancelWorkflowCommand(
    Guid WorkflowInstanceId,
    Guid CancelledBy) : IRequest<Result>;
