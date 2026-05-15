using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class StepInstance : BaseEntity<Guid>
{
    public Guid WorkflowInstanceId { get; private set; }
    public Guid StepDefinitionId { get; private set; }
    public string StepName { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }
    public StepStatus Status { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private StepInstance() { }

    internal static StepInstance Create(
        Guid workflowInstanceId,
        StepDefinition definition)
    {
        return new StepInstance
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            StepDefinitionId = definition.Id,
            StepName = definition.Name,
            Order = definition.Order,
            IsRequired = definition.IsRequired,
            Status = StepStatus.Pending,
        };
    }

    internal void Assign(Guid assigneeId)
    {
        if (Status != StepStatus.Pending)
            throw new WorkflowDomainException($"Step '{StepName}' cannot be assigned in its current state ({Status}).");

        AssigneeId = assigneeId;
        Status = StepStatus.Assigned;
    }

    internal void Complete()
    {
        if (Status is not (StepStatus.Assigned or StepStatus.InProgress or StepStatus.Pending))
            throw new WorkflowDomainException($"Step '{StepName}' cannot be completed in its current state ({Status}).");

        Status = StepStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    internal void Fail(string reason)
    {
        if (Status is StepStatus.Completed or StepStatus.Skipped)
            throw new WorkflowDomainException($"Step '{StepName}' cannot be failed in its current state ({Status}).");

        if (string.IsNullOrWhiteSpace(reason))
            throw new WorkflowDomainException("A failure reason must be provided.");

        Status = StepStatus.Failed;
        FailureReason = reason.Trim();
        CompletedAt = DateTime.UtcNow;
    }

    internal void Skip()
    {
        if (Status is StepStatus.Completed or StepStatus.Failed or StepStatus.Skipped)
            throw new WorkflowDomainException($"Step '{StepName}' cannot be skipped in its current state ({Status}).");

        if (IsRequired)
            throw new WorkflowDomainException($"Required step '{StepName}' cannot be skipped.");

        Status = StepStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
    }

    internal bool IsTerminal =>
        Status is StepStatus.Completed or StepStatus.Skipped or StepStatus.Failed;
}
