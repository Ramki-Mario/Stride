using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Workflows.Domain.Events;

public sealed record StepsRevertedEvent(
    Guid   WorkflowInstanceId,
    Guid   TenantId,
    Guid   RejectedStepInstanceId,
    string RejectedStepName,
    int    TargetStepOrder,
    IReadOnlyList<(Guid StepInstanceId, string StepName)> RevertedSteps,
    Guid   RejectedBy,
    string? Comment) : IDomainEvent;
