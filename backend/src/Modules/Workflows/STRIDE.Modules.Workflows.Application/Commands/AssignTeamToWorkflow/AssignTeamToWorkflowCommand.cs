using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.AssignTeamToWorkflow;

/// <summary>
/// Assigns (or clears) a team on a workflow instance.
/// Pass a null <see cref="TeamId"/> to remove the current assignment.
/// </summary>
public sealed record AssignTeamToWorkflowCommand(
    Guid  WorkflowInstanceId,
    Guid? TeamId,
    Guid  AssignedBy,
    Guid  TenantId) : IRequest<Result>;
