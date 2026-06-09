using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record ApprovalRequestedEvent(
    Guid   StepInstanceId,
    Guid   WorkflowInstanceId,
    Guid   TenantId,
    Guid?  RequiredRoleId,
    string StepName        = "",
    string WorkflowName    = "") : IDomainEvent;
