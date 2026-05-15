using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.DeleteWorkflow;

/// <summary>
/// Soft-deletes a workflow definition. Only Draft definitions can be deleted;
/// Active or Running definitions must be archived or cancelled first.
/// </summary>
public sealed record DeleteWorkflowCommand(
    Guid WorkflowDefinitionId,
    Guid DeletedBy) : IRequest<Result>;
