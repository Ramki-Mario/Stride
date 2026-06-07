using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class WorkflowInstance : AuditableEntity
{
    private readonly List<StepInstance> _steps = new();

    public Guid  WorkflowDefinitionId { get; private set; }
    public string WorkflowName { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; }
    public Guid  StartedBy { get; private set; }
    public Guid? ClientId  { get; private set; }       // optional link to a Clients.Client
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<StepInstance> Steps => _steps.AsReadOnly();

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

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            TenantId = definition.TenantId,
            WorkflowDefinitionId = definition.Id,
            WorkflowName = definition.Name,
            Status = WorkflowStatus.Running,
            StartedBy = startedBy,
            ClientId  = clientId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = startedBy,
        };

        // Snapshot steps from definition at start time (order preserved)
        foreach (var stepDef in definition.Steps.OrderBy(s => s.Order))
        {
            instance._steps.Add(StepInstance.Create(instance.Id, instance.TenantId, stepDef));
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
        if (Status is not (WorkflowStatus.Running or WorkflowStatus.Paused))
            throw new WorkflowDomainException("Only Running or Paused workflows can be cancelled.");

        Status = WorkflowStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowCancelledEvent(Id, TenantId, cancelledBy));
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

        RaiseDomainEvent(new StepAssignedEvent(step.Id, Id, TenantId, assigneeId, assignedBy));
    }

    public void CompleteStep(
        Guid stepInstanceId,
        Guid completedBy,
        IReadOnlyList<(string Description, decimal Quantity, decimal UnitPrice, BillableUnit Unit)>? billableItems = null)
    {
        EnsureRunning();

        var step = GetStep(stepInstanceId);
        step.Complete(billableItems);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new StepCompletedEvent(step.Id, Id, TenantId, completedBy));
        CheckCompletion();
    }

    public void FailStep(Guid stepInstanceId, string reason, Guid failedBy)
    {
        EnsureRunning();

        var step = GetStep(stepInstanceId);
        step.Fail(reason);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new StepFailedEvent(step.Id, Id, TenantId, reason, failedBy));

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

        RaiseDomainEvent(new StepSkippedEvent(step.Id, Id, TenantId, skippedBy));
        CheckCompletion();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private void EnsureRunning()
    {
        if (Status != WorkflowStatus.Running)
            throw new WorkflowDomainException($"Step operations require a Running workflow (current: {Status}).");
    }

    private StepInstance GetStep(Guid stepInstanceId) =>
        _steps.FirstOrDefault(s => s.Id == stepInstanceId)
            ?? throw new WorkflowDomainException($"Step instance '{stepInstanceId}' not found in this workflow.");

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
