using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

/// <summary>
/// Raised when a team is assigned to (or cleared from) a workflow instance.
/// <para>
/// <see cref="TeamId"/> is <c>null</c> when the team assignment is removed.
/// </para>
/// </summary>
public sealed record WorkflowTeamAssignedEvent(
    Guid  WorkflowInstanceId,
    Guid  TenantId,
    Guid? TeamId,
    Guid  AssignedBy) : IDomainEvent;
