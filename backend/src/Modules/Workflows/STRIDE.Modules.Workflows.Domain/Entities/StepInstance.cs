using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class StepInstance : BaseEntity<Guid>
{
    private readonly List<BillableItem> _billableItems = new();
    private readonly List<StepFieldValue> _fieldValues = new();

    public Guid   WorkflowInstanceId { get; private set; }
    public Guid   TenantId           { get; private set; }
    public Guid   StepDefinitionId   { get; private set; }
    public string StepName           { get; private set; } = string.Empty;
    public int    Order              { get; private set; }
    public bool   IsRequired         { get; private set; }
    public StepStatus Status         { get; private set; }
    public Guid?  AssigneeId         { get; private set; }
    public DateTime? AssignedAt      { get; private set; }
    /// <summary>Cross-module FK — bare Guid, no EF navigation across module boundary.</summary>
    public Guid?  RequiredRoleId     { get; private set; }
    public string? FailureReason     { get; private set; }
    public DateTime? CompletedAt     { get; private set; }

    /// <summary>
    /// Snapshotted from the step definition at instance creation.
    /// Hours allowed from the preceding step's completion (or instance start for step 1).
    /// Null means no deadline for this step.
    /// </summary>
    public decimal? DueOffsetHours   { get; private set; }

    /// <summary>
    /// Absolute deadline for this step. Calculated on instance creation for step 1;
    /// recalculated when the preceding step completes for all subsequent steps.
    /// Null when no DueOffsetHours was configured.
    /// </summary>
    public DateTime? DueAt           { get; private set; }

    /// <summary>
    /// Set to <c>true</c> by the background deadline checker once DueAt has passed and the step
    /// is still incomplete. Prevents duplicate overdue notifications.
    /// </summary>
    public bool IsOverdue { get; private set; }

    /// <summary>Timestamp at which the overdue notification was sent. Null until IsOverdue is set.</summary>
    public DateTime? OverdueNotifiedAt { get; private set; }

    /// <summary>Snapshotted step type — Standard or Approval gate.</summary>
    public StepType StepType { get; private set; }

    /// <summary>Snapshotted rejection handling strategy for Approval steps.</summary>
    public RejectionHandling RejectionHandling { get; private set; }

    /// <summary>Snapshotted target step Order for RevertToStep rejection handling.</summary>
    public int? RevertToStepOrder { get; private set; }

    /// <summary>Billable items logged when this step was completed.</summary>
    public IReadOnlyList<BillableItem> BillableItems => _billableItems.AsReadOnly();

    /// <summary>Runtime field values captured when this step was completed.</summary>
    public IReadOnlyList<StepFieldValue> FieldValues => _fieldValues.AsReadOnly();

    private StepInstance() { }

    internal static StepInstance Create(
        Guid workflowInstanceId,
        Guid tenantId,
        StepDefinition definition,
        DateTime? dueAt = null)
    {
        return new StepInstance
        {
            Id                 = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            TenantId           = tenantId,
            StepDefinitionId   = definition.Id,
            StepName           = definition.Name,
            Order              = definition.Order,
            IsRequired         = definition.IsRequired,
            RequiredRoleId     = definition.RequiredRoleId,
            DueOffsetHours     = definition.DueOffsetHours,
            DueAt              = dueAt,
            Status             = StepStatus.Pending,
            StepType           = definition.StepType,
            RejectionHandling  = definition.RejectionHandling,
            RevertToStepOrder  = definition.RevertToStepOrder,
        };
    }

    /// <summary>
    /// Sets or updates the absolute due date for this step.
    /// Called by the workflow aggregate when the preceding step completes.
    /// </summary>
    internal void SetDueAt(DateTime? dueAt) => DueAt = dueAt;

    /// <summary>
    /// Flags this step as overdue. Called by the <c>WorkflowInstance</c> aggregate in response
    /// to the deadline-checker background job. No-op if already flagged.
    /// </summary>
    internal void MarkOverdue()
    {
        if (IsOverdue) return;
        IsOverdue          = true;
        OverdueNotifiedAt  = DateTime.UtcNow;
    }

    internal void Assign(Guid assigneeId)
    {
        if (Status != StepStatus.Pending)
            throw new WorkflowDomainException($"Step '{StepName}' cannot be assigned in its current state ({Status}).");

        AssigneeId = assigneeId;
        AssignedAt = DateTime.UtcNow;
        Status     = StepStatus.Assigned;
    }

    /// <summary>
    /// Marks the step as completed and optionally records billable items.
    /// Items with zero quantity or zero price are rejected at the domain level.
    /// </summary>
    internal void Complete(
        IReadOnlyList<(string Description, decimal Quantity, decimal UnitPrice, BillableUnit Unit)>? billableItems = null,
        IReadOnlyList<(Guid StepFieldDefinitionId, string Value)>? fieldValues = null)
    {
        if (Status is not (StepStatus.Assigned or StepStatus.InProgress or StepStatus.Pending))
            throw new WorkflowDomainException($"Step '{StepName}' cannot be completed in its current state ({Status}).");

        if (billableItems is not null)
        {
            foreach (var (desc, qty, price, unit) in billableItems)
            {
                _billableItems.Add(BillableItem.Create(Id, TenantId, desc, qty, price, unit));
            }
        }

        if (fieldValues is not null)
        {
            var duplicateField = fieldValues
                .GroupBy(v => v.StepFieldDefinitionId)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateField is not null)
                throw new WorkflowDomainException(
                    $"Field '{duplicateField.Key}' was submitted more than once for step '{StepName}'.");

            foreach (var (fieldDefinitionId, value) in fieldValues)
            {
                _fieldValues.Add(StepFieldValue.Create(Id, TenantId, fieldDefinitionId, value));
            }
        }

        Status      = StepStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    internal void Fail(string reason)
    {
        if (Status is StepStatus.Completed or StepStatus.Skipped)
            throw new WorkflowDomainException($"Step '{StepName}' cannot be failed in its current state ({Status}).");

        if (string.IsNullOrWhiteSpace(reason))
            throw new WorkflowDomainException("A failure reason must be provided.");

        Status        = StepStatus.Failed;
        FailureReason = reason.Trim();
        CompletedAt   = DateTime.UtcNow;
    }

    internal void Skip()
    {
        if (Status is StepStatus.Completed or StepStatus.Failed or StepStatus.Skipped)
            throw new WorkflowDomainException($"Step '{StepName}' cannot be skipped in its current state ({Status}).");

        if (IsRequired)
            throw new WorkflowDomainException($"Required step '{StepName}' cannot be skipped.");

        Status      = StepStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
    }

    // ── Approval-gate methods ─────────────────────────────────────────────────

    /// <summary>
    /// Transitions a Pending Approval-type step to AwaitingApproval and returns
    /// the new <see cref="ApprovalRequest"/> to be added to the aggregate.
    /// </summary>
    internal ApprovalRequest ActivateForApproval()
    {
        if (StepType != StepType.Approval)
            throw new WorkflowDomainException($"Step '{StepName}' is not an Approval step.");

        if (Status != StepStatus.Pending)
            throw new WorkflowDomainException($"Step '{StepName}' must be Pending to activate for approval (current: {Status}).");

        Status = StepStatus.AwaitingApproval;

        return ApprovalRequest.Create(
            WorkflowInstanceId,
            Id,
            TenantId,
            RequiredRoleId,
            RejectionHandling,
            RevertToStepOrder);
    }

    /// <summary>Transitions AwaitingApproval → Completed when the approval request is approved.</summary>
    internal void MarkApproved()
    {
        if (Status != StepStatus.AwaitingApproval)
            throw new WorkflowDomainException($"Step '{StepName}' is not awaiting approval (current: {Status}).");

        Status      = StepStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Transitions AwaitingApproval → Rejected when the approval request is rejected.</summary>
    internal void MarkRejected()
    {
        if (Status != StepStatus.AwaitingApproval)
            throw new WorkflowDomainException($"Step '{StepName}' is not awaiting approval (current: {Status}).");

        Status      = StepStatus.Rejected;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets a step to Pending so it can be re-completed after an approval rejection
    /// with RevertToStep handling.
    /// </summary>
    internal void ResetToPending()
    {
        Status        = StepStatus.Pending;
        AssigneeId    = null;
        AssignedAt    = null;
        CompletedAt   = null;
        FailureReason = null;
    }

    internal bool IsTerminal =>
        Status is StepStatus.Completed or StepStatus.Skipped or StepStatus.Failed or StepStatus.Rejected;
}
