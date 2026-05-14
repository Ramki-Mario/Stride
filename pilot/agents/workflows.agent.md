# workflows.agent.md
# STRIDE — Workflows Agent

## Purpose
Persistent AI memory for workflow, task, and domain event implementation.
Read before implementing any workflow, approval, state machine, or event-driven logic.

---

## Workflow Lifecycle States

```
New
  → Assigned
    → InProgress
      → PendingApproval
        → Approved  → Completed → Invoiced
        → Rejected  → InProgress (resubmit)
      → Completed (no approval required)
    → Cancelled (from InProgress, with audit)
  → Cancelled (from Assigned)
→ Cancelled (from New)
```

State transitions must be:
- Validated in the Domain layer (not Application or API)
- Logged with TenantId, UserId, CorrelationId, old state, new state
- Emitted as domain events on every transition

---

## Domain Event Flow

When a workflow changes state, the aggregate:
1. Updates its own state
2. Raises a `IDomainEvent` (e.g., `WorkflowStatusChangedDomainEvent`)
3. The Application layer dispatches domain events via MediatR after the write succeeds
4. Domain event handlers may trigger: notifications, audit logs, integration events

```csharp
// Domain layer
public class Workflow : AuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void TransitionTo(WorkflowStatus newStatus, Guid changedBy)
    {
        ValidateTransition(_status, newStatus); // throws if invalid
        _status = newStatus;
        _domainEvents.Add(new WorkflowStatusChangedDomainEvent(Id, TenantId, newStatus, changedBy));
    }
}
```

---

## Integration Events (Cross-Module)

When a workflow event needs to notify another module:
- Raise an `IIntegrationEvent` (e.g., `WorkflowCompletedIntegrationEvent`)
- Publish via `IEventBus`
- Other modules subscribe — they do NOT directly call the Workflow module

Examples:
- `WorkflowCompletedIntegrationEvent` → Invoicing module starts invoice generation
- `WorkflowAssignedIntegrationEvent` → Notifications module sends assignment alert

---

## MediatR Command/Query Structure

```
Application/
├── Commands/
│   ├── CreateWorkflow/
│   │   ├── CreateWorkflowCommand.cs
│   │   └── CreateWorkflowCommandHandler.cs
│   ├── AssignWorkflow/
│   │   ├── AssignWorkflowCommand.cs
│   │   └── AssignWorkflowCommandHandler.cs
│   └── TransitionWorkflowStatus/
│       ├── TransitionWorkflowStatusCommand.cs
│       └── TransitionWorkflowStatusCommandHandler.cs
├── Queries/
│   ├── GetWorkflowById/
│   │   ├── GetWorkflowByIdQuery.cs
│   │   └── GetWorkflowByIdQueryHandler.cs
│   └── GetWorkflowList/
│       ├── GetWorkflowListQuery.cs
│       └── GetWorkflowListQueryHandler.cs
└── EventHandlers/
    ├── WorkflowStatusChangedDomainEventHandler.cs
    └── WorkflowCompletedIntegrationEventHandler.cs
```

---

## Approval Engine

Approval workflow rules:
- Certain workflow types require approval before completion
- Approval requests are created as child entities of the workflow
- Approval can be: Approved, Rejected, EscalationRequested
- Multi-level approvals are supported via ordered approval steps
- Approval timeout policy (future): auto-escalate after X hours

---

## Audit Logging Requirements

Every workflow state change MUST produce an audit record containing:
```
TenantId
WorkflowId
ActionType (Created, Assigned, StatusChanged, Approved, Rejected, Cancelled)
OldStatus
NewStatus
PerformedBy (UserId)
PerformedAt (UTC)
CorrelationId
Notes (optional)
```

Audit records are immutable — never update or delete.

---

## Scheduling Module Boundary

The Scheduling module handles:
- Workforce assignment to workflows
- Operational scheduling and time slots
- Execution tracking

The Scheduling module communicates with Workflows via `IEventBus` integration events.
It does NOT directly reference `STRIDE.Modules.Workflows.*` projects.

---

## AI Constraints

MUST:
- Validate state transitions in the Domain layer
- Raise domain events on every meaningful state change
- Log every state transition with full context
- Use MediatR for command/query dispatch within the module
- Use IEventBus for cross-module communication

MUST NOT:
- Allow invalid state transitions silently
- Reference other module projects directly
- Put state machine logic in controllers or handlers
- Skip audit logging for any workflow action
