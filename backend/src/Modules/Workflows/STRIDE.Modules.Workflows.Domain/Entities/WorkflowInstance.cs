using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class WorkflowInstance : AuditableEntity
{
    private readonly List<StepInstance>     _steps           = new();
    private readonly List<ApprovalRequest>  _approvalRequests = new();

    public Guid  WorkflowDefinitionId { get; private set; }
    public string WorkflowName { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; }
    public Guid  StartedBy { get; private set; }
    public Guid? ClientId  { get; private set; }       // optional link to a Clients.Client
    public Guid? TeamId    { get; private set; }       // optional link to a Teams.Team
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Absolute SLA deadline for the entire workflow instance.
    /// Calculated as <c>CreatedAt + definition.SlaOffsetHours</c> when the instance is started.
    /// Null when no SLA was configured on the definition.
    /// </summary>
    public DateTime? DeadlineAt { get; private set; }

    /// <summary>
    /// Set to <c>true</c> by the background deadline checker once DeadlineAt has passed
    /// and the workflow is still running. Prevents duplicate SLA-breach notifications.
    /// </summary>
    public bool IsSlaBreached { get; private set; }

    /// <summary>Timestamp at which the SLA-breach notification was sent. Null until IsSlaBreached is set.</summary>
    public DateTime? SlaBreachedNotifiedAt { get; private set; }

    public IReadOnlyList<StepInstance>    Steps            => _steps.AsReadOnly();
    public IReadOnlyList<ApprovalRequest> ApprovalRequests => _approvalRequests.AsReadOnly();

    private WorkflowInstance() { }

    public static WorkflowInstance Start(
        WorkflowDefinition definition,
        Guid  startedBy,
        Guid? clientId = null)
    {
        if (definition.Status != WorkflowStatus.Active)
            throw new WorkflowDomainException("Only Active workflow definitions can be started.");

        if (definition.Steps.Count == 0)
            throw new WorkflowDomainException("Cannot start a workflow with no steps.");

        var startedAt = DateTime.UtcNow;
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            TenantId = definition.TenantId,
            WorkflowDefinitionId = definition.Id,
            WorkflowName = definition.Name,
            Status = WorkflowStatus.Running,
            StartedBy = startedBy,
            ClientId  = clientId,
            CreatedAt = startedAt,
            UpdatedAt = startedAt,
            CreatedBy = startedBy,
            DeadlineAt = definition.SlaOffsetHours.HasValue
                ? startedAt.AddHours((double)definition.SlaOffsetHours.Value)
                : (DateTime?)null,
        };

        // Snapshot steps from definition at start time.
        // The first step's due date is calculated from the instance start time;
        // all subsequent steps are calculated when their predecessor completes.
        foreach (var stepDef in definition.Steps.OrderBy(s => s.Order))
        {
            var dueAt = stepDef.Order == 0 && stepDef.DueOffsetHours.HasValue
                ? startedAt.AddHours((double)stepDef.DueOffsetHours.Value)
                : (DateTime?)null;

            instance._steps.Add(StepInstance.Create(instance.Id, instance.TenantId, stepDef, dueAt));
        }

        // If the first step is an Approval gate, activate it immediately.
        var firstStep = instance._steps.OrderBy(s => s.Order).FirstOrDefault();
        if (firstStep?.StepType == StepType.Approval)
        {
            var request = firstStep.ActivateForApproval();
            instance._approvalRequests.Add(request);
            instance.RaiseDomainEvent(new ApprovalRequestedEvent(
                firstStep.Id, instance.Id, instance.TenantId, firstStep.RequiredRoleId, firstStep.StepName, instance.WorkflowName));
        }

        instance.RaiseDomainEvent(new WorkflowStartedEvent(
            instance.Id,
            instance.TenantId,
            definition.Id,
            instance.WorkflowName,
            startedBy));

        return instance;
    }

    public void Pause(Guid pausedBy)
    {
        if (Status != WorkflowStatus.Running)
            throw new WorkflowDomainException("Only Running workflows can be paused.");

        Status = WorkflowStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowPausedEvent(Id, TenantId, pausedBy));
    }

    public void Resume(Guid resumedBy)
    {
        if (Status != WorkflowStatus.Paused)
            throw new WorkflowDomainException("Only Paused workflows can be resumed.");

        Status = WorkflowStatus.Running;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowResumedEvent(Id, TenantId, resumedBy));
    }

    public void Cancel(Guid cancelledBy)
    {
        if (Status is not (WorkflowStatus.Running or WorkflowStatus.Paused or WorkflowStatus.Halted))
            throw new WorkflowDomainException("Only Running, Paused, or Halted workflows can be cancelled.");

        Status = WorkflowStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowCancelledEvent(Id, TenantId, cancelledBy));
    }

    /// <summary>
    /// Resumes a Halted workflow (rejected with HaltWorkflow handling) back to Running.
    /// Only managers should be permitted to call this; the role check belongs in the command handler.
    /// </summary>
    public void ResumeFromHalt(Guid resumedBy)
    {
        if (Status != WorkflowStatus.Halted)
            throw new WorkflowDomainException("Only Halted workflows can be resumed from halt.");

        Status = WorkflowStatus.Running;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowResumedEvent(Id, TenantId, resumedBy));
    }

    /// <summary>
    /// Assigns (or removes) a team from this workflow instance.
    /// A null <paramref name="teamId"/> clears the current team assignment.
    /// Can be called on any non-deleted instance regardless of status.
    /// </summary>
    public void AssignTeam(Guid? teamId, Guid assignedBy)
    {
        TeamId    = teamId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowTeamAssignedEvent(Id, TenantId, teamId, assignedBy));
    }

    public void Fail(string reason, Guid failedBy)
    {
        if (Status is not (WorkflowStatus.Running or WorkflowStatus.Paused))
            throw new WorkflowDomainException("Only Running or Paused workflows can be failed.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new WorkflowDomainException("A failure reason must be provided.");

        Status = WorkflowStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowFailedEvent(Id, TenantId, reason, failedBy));
    }

    public void AssignStep(Guid stepInstanceId, Guid assigneeId, Guid assignedBy)
    {
        EnsureRunning();

        var step = GetStep(stepInstanceId);
        step.Assign(assigneeId);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new StepAssignedEvent(step.Id, Id, TenantId, assigneeId, assignedBy, step.StepName));
    }

    public void CompleteStep(
        Guid stepInstanceId,
        Guid completedBy,
        IReadOnlyList<(string Description, decimal Quantity, decimal UnitPrice, BillableUnit Unit)>? billableItems = null,
        IReadOnlyList<(Guid StepFieldDefinitionId, string Value)>? fieldValues = null)
    {
        EnsureRunning();

        var step = GetStep(stepInstanceId);
        step.Complete(billableItems, fieldValues);
        UpdatedAt = DateTime.UtcNow;

        // When a step completes, recalculate the next pending step's due date
        // from this step's actual completion time (handles late completions).
        RecalculateNextStepDueDate(step);
        ActivateNextApprovalStepIfNeeded(step);

        RaiseDomainEvent(new StepCompletedEvent(step.Id, Id, TenantId, completedBy, step.StepName));
        CheckCompletion();
    }

    public void FailStep(Guid stepInstanceId, string reason, Guid failedBy)
    {
        EnsureRunning();

        var step = GetStep(stepInstanceId);
        step.Fail(reason);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new StepFailedEvent(step.Id, Id, TenantId, reason, failedBy, step.StepName));

        // A required step failing fails the entire workflow
        if (step.IsRequired)
        {
            Status = WorkflowStatus.Failed;
            RaiseDomainEvent(new WorkflowFailedEvent(Id, TenantId, $"Required step '{step.StepName}' failed: {reason}", failedBy));
        }
    }

    public void SkipStep(Guid stepInstanceId, Guid skippedBy)
    {
        EnsureRunning();

        var step = GetStep(stepInstanceId);
        step.Skip();
        UpdatedAt = DateTime.UtcNow;

        RecalculateNextStepDueDate(step);
        ActivateNextApprovalStepIfNeeded(step);

        RaiseDomainEvent(new StepSkippedEvent(step.Id, Id, TenantId, skippedBy, step.StepName));
        CheckCompletion();
    }

    // ── Approval-gate operations ───────────────────────────────────────────────

    /// <summary>
    /// Approves the pending approval request on the given step.
    /// The step transitions AwaitingApproval → Completed and the next step is activated.
    /// </summary>
    public void ApproveStep(Guid stepInstanceId, Guid approvedBy, string? comment = null)
    {
        EnsureRunning();

        var step    = GetStep(stepInstanceId);
        var request = GetPendingApprovalRequest(stepInstanceId);

        request.Approve(approvedBy, comment);
        step.MarkApproved();
        UpdatedAt = DateTime.UtcNow;

        RecalculateNextStepDueDate(step);
        ActivateNextApprovalStepIfNeeded(step);

        RaiseDomainEvent(new StepApprovedEvent(step.Id, Id, TenantId, approvedBy, step.StepName));
        CheckCompletion();
    }

    /// <summary>
    /// Rejects the pending approval request on the given step.
    /// Depending on <see cref="RejectionHandling"/>, the workflow either halts or reverts to an earlier step.
    /// </summary>
    public void RejectStep(Guid stepInstanceId, Guid rejectedBy, string? comment = null)
    {
        EnsureRunning();

        var step    = GetStep(stepInstanceId);
        var request = GetPendingApprovalRequest(stepInstanceId);

        request.Reject(rejectedBy, comment);
        step.MarkRejected();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new StepRejectedEvent(step.Id, Id, TenantId, rejectedBy, step.StepName));

        if (request.RejectionHandling == RejectionHandling.HaltWorkflow)
        {
            Status = WorkflowStatus.Halted;
            RaiseDomainEvent(new WorkflowHaltedEvent(Id, TenantId, step.Id, rejectedBy));
        }
        else // RevertToStep
        {
            var targetStep = _steps.FirstOrDefault(s => s.Order == request.RevertToStepOrder);
            if (targetStep is null)
                throw new WorkflowDomainException($"Revert target step (order {request.RevertToStepOrder}) not found.");

            // Mark all completed intermediate steps (between target exclusive and rejected exclusive) as Reverted.
            var intermediateSteps = _steps
                .Where(s => s.Order > targetStep.Order && s.Order < step.Order && s.IsTerminal)
                .ToList();

            foreach (var intermediate in intermediateSteps)
                intermediate.MarkReverted();

            targetStep.ResetToPending();

            if (intermediateSteps.Count > 0)
            {
                var revertedInfos = intermediateSteps
                    .Select(s => (s.Id, s.StepName))
                    .ToList()
                    .AsReadOnly() as IReadOnlyList<(Guid StepInstanceId, string StepName)>;

                RaiseDomainEvent(new StepsRevertedEvent(
                    Id, TenantId, step.Id, step.StepName,
                    targetStep.Order, revertedInfos!, rejectedBy, comment));
            }
        }
    }

    // ── Deadline / overdue tracking ────────────────────────────────────────────

    /// <summary>
    /// Flags the specified step as overdue and raises a <see cref="StepOverdueEvent"/>.
    /// Called by the background deadline-checker job via <see cref="WorkflowsDbContext"/>.
    /// No-op when the step has already been flagged or has reached a terminal state.
    /// </summary>
    public void MarkStepOverdue(Guid stepId)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null || step.IsTerminal || step.IsOverdue) return;

        step.MarkOverdue();
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new StepOverdueEvent(step.Id, Id, TenantId, step.AssigneeId, StartedBy, step.StepName));
    }

    /// <summary>
    /// Flags the entire workflow instance's SLA as breached and raises a
    /// <see cref="WorkflowSlaBreachedEvent"/>.
    /// Called by the background deadline-checker job.
    /// No-op if already breached, the workflow has no deadline, or the workflow is not running.
    /// </summary>
    public void MarkSlaBreached()
    {
        if (IsSlaBreached || DeadlineAt is null || Status != WorkflowStatus.Running) return;

        IsSlaBreached           = true;
        SlaBreachedNotifiedAt   = DateTime.UtcNow;
        UpdatedAt               = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowSlaBreachedEvent(Id, TenantId, StartedBy, DeadlineAt.Value));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// When a step completes, finds the immediately next non-terminal step and
    /// sets its DueAt from the actual completion time. This correctly handles
    /// steps that complete late, cascading the new baseline forward.
    /// </summary>
    private void RecalculateNextStepDueDate(StepInstance completedStep)
    {
        if (completedStep.CompletedAt is null) return;

        var nextStep = _steps
            .Where(s => s.Order > completedStep.Order && !s.IsTerminal)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        if (nextStep is not null && nextStep.DueOffsetHours.HasValue)
            nextStep.SetDueAt(completedStep.CompletedAt.Value.AddHours((double)nextStep.DueOffsetHours.Value));
    }

    /// <summary>
    /// After a step becomes terminal, checks if the immediately next Pending step is an
    /// Approval gate and auto-activates it by creating an ApprovalRequest.
    /// </summary>
    private void ActivateNextApprovalStepIfNeeded(StepInstance completedStep)
    {
        var nextStep = _steps
            .Where(s => s.Order > completedStep.Order && s.Status == StepStatus.Pending)
            .OrderBy(s => s.Order)
            .FirstOrDefault();

        if (nextStep?.StepType == StepType.Approval)
        {
            var request = nextStep.ActivateForApproval();
            _approvalRequests.Add(request);
            RaiseDomainEvent(new ApprovalRequestedEvent(
                nextStep.Id, Id, TenantId, nextStep.RequiredRoleId, nextStep.StepName, WorkflowName));
        }
    }

    private void EnsureRunning()
    {
        if (Status != WorkflowStatus.Running)
            throw new WorkflowDomainException($"Step operations require a Running workflow (current: {Status}).");
    }

    private StepInstance GetStep(Guid stepInstanceId) =>
        _steps.FirstOrDefault(s => s.Id == stepInstanceId)
            ?? throw new WorkflowDomainException($"Step instance '{stepInstanceId}' not found in this workflow.");

    private ApprovalRequest GetPendingApprovalRequest(Guid stepInstanceId) =>
        _approvalRequests.FirstOrDefault(a => a.StepInstanceId == stepInstanceId && a.Status == ApprovalStatus.Pending)
            ?? throw new WorkflowDomainException($"No pending approval request found for step '{stepInstanceId}'.");

    private void CheckCompletion()
    {
        if (_steps.All(s => s.IsTerminal))
        {
            Status = WorkflowStatus.Completed;
            CompletedAt = DateTime.UtcNow;

            // Snapshot all billable items so downstream handlers (e.g. Invoicing) can
            // create an invoice draft without a cross-module query.
            var billableSnapshots = _steps
                .SelectMany(s => s.BillableItems)
                .Select(b => new WorkflowBillableItemSnapshot(
                    b.Description,
                    b.Quantity,
                    b.UnitPrice,
                    b.Unit.ToString()))
                .ToList();

            RaiseDomainEvent(new WorkflowCompletedEvent(
                Id, TenantId, StartedBy, WorkflowName, ClientId, billableSnapshots));
        }
    }
}
