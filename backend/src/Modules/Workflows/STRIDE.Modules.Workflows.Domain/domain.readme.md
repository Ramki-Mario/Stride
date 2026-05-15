# Workflow Domain Model — KT Document

> **Story:** EP-018 · US-041 · US-042 · US-043  
> **Layer:** Domain (no infrastructure / framework references)  
> **Namespace root:** `STRIDE.Modules.Workflows.Domain`

---

## Directory Structure

```
STRIDE.Modules.Workflows.Domain/
├── Entities/
│   ├── WorkflowDefinition.cs   ← Aggregate root (template)
│   ├── StepDefinition.cs       ← Child entity of WorkflowDefinition
│   ├── WorkflowInstance.cs     ← Aggregate root (runtime)
│   └── StepInstance.cs         ← Child entity of WorkflowInstance
├── Enums/
│   ├── WorkflowStatus.cs
│   └── StepStatus.cs
├── Events/                     ← Domain events (IDomainEvent sealed records)
│   ├── WorkflowCreatedEvent.cs
│   ├── WorkflowActivatedEvent.cs
│   ├── WorkflowStartedEvent.cs
│   ├── WorkflowPausedEvent.cs
│   ├── WorkflowResumedEvent.cs
│   ├── WorkflowCompletedEvent.cs
│   ├── WorkflowCancelledEvent.cs
│   ├── WorkflowFailedEvent.cs
│   ├── StepAssignedEvent.cs
│   ├── StepCompletedEvent.cs
│   ├── StepFailedEvent.cs
│   └── StepSkippedEvent.cs
└── Exceptions/
    └── WorkflowDomainException.cs
```

---

## Core Concepts

### Two Aggregate Roots

| Aggregate | Role |
|-----------|------|
| `WorkflowDefinition` | **Template** — defines the shape of a workflow (name, ordered steps). Created by admins. Must be Activated before use. |
| `WorkflowInstance` | **Runtime** — a live execution of a definition. Snapshots step definitions at start time so the template can change without affecting running instances. |

### Child Entities

| Entity | Parent | Access |
|--------|--------|--------|
| `StepDefinition` | `WorkflowDefinition` | `internal static Create()` — only created by the aggregate root |
| `StepInstance` | `WorkflowInstance` | `internal` methods — only mutated through `WorkflowInstance` |

> **DDD Rule:** Never mutate child entities directly from outside the aggregate. Always go through the aggregate root methods (`AssignStep`, `CompleteStep`, etc.).

---

## State Machines

### WorkflowStatus

```
Draft ──Activate()──► Active ──Start()──► Running ──Pause()──► Paused
  │                                          │                    │
Delete()                                  Cancel()            Resume()──► Running
  │                                       Fail()                  │
  ▼                                          │                 Cancel()
IsDeleted                                    ▼                 Fail()
                                        Completed                 │
                                        Cancelled                 ▼
                                        Failed                  ...
                                        Archived (from any terminal state)
```

### StepStatus

```
Pending ──Assign()──► Assigned ──► InProgress*
   │                     │              │
   └──────Complete()──────┴──Complete()─┘
   │                                    ▼
   │                               Completed
   └──Skip()──► Skipped (only non-required steps)
   └──Fail()──► Failed
```

> *InProgress is a status value reserved for future use (e.g., external system integrations). Currently, steps transition Assigned → Completed directly.

---

## Key Design Decisions

### 1. Step Snapshot on Start
`WorkflowInstance.Start()` copies all `StepDefinition` data into `StepInstance` records. This means:
- Editing the template after an instance starts has **no effect** on running instances.
- Each `StepInstance` carries `StepName`, `Order`, `IsRequired` as its own data.

### 2. Required vs Optional Steps
- `StepDefinition.IsRequired = true` (default) — if this step fails, the entire `WorkflowInstance` is also marked `Failed` and a `WorkflowFailedEvent` is raised automatically.
- Optional steps (`IsRequired = false`) can be **skipped** without failing the workflow.

### 3. Auto-Completion (`CheckCompletion`)
After every `CompleteStep()` or `SkipStep()`, `WorkflowInstance` privately checks whether all steps are terminal (`Completed | Skipped | Failed`). If so, it transitions to `Completed` and raises `WorkflowCompletedEvent`.

### 4. Domain Events
All state transitions raise domain events via `RaiseDomainEvent()` (inherited from `AuditableEntity`). Events are:
- Collected in a private `List<IDomainEvent>` on the aggregate.
- Dispatched by the infrastructure layer (EF Core SaveChanges interceptor or MediatR pipeline — see EP-020).
- Cleared after dispatch via `ClearDomainEvents()`.

### 5. Guard Pattern
Every state-transition method begins with a guard. Example:
```csharp
public void Pause(Guid pausedBy)
{
    if (Status != WorkflowStatus.Running)
        throw new WorkflowDomainException("Only Running workflows can be paused.");
    ...
}
```
All guards throw `WorkflowDomainException` (not ArgumentException or InvalidOperationException), keeping the error type domain-specific so the application layer can catch and map to `Result.Failure`.

---

## Enums

### WorkflowStatus
| Value | Int | Meaning |
|-------|-----|---------|
| Draft | 0 | Being designed; editable |
| Active | 1 | Ready to be instantiated |
| Running | 2 | Live instance in progress |
| Paused | 3 | Temporarily halted |
| Completed | 4 | All steps finished |
| Cancelled | 5 | Manually cancelled |
| Failed | 6 | A required step failed |
| Archived | 7 | Retired definition |

### StepStatus
| Value | Int | Meaning |
|-------|-----|---------|
| Pending | 0 | Not yet started |
| Assigned | 1 | Assigned to a user |
| InProgress | 2 | Reserved / future |
| Completed | 3 | Done |
| Skipped | 4 | Deliberately skipped (optional only) |
| Failed | 5 | Errored |

---

## Domain Events Reference

| Event | Raised By | Key Payload |
|-------|-----------|-------------|
| `WorkflowCreatedEvent` | `WorkflowDefinition.Create` | DefinitionId, Name, CreatedBy |
| `WorkflowActivatedEvent` | `WorkflowDefinition.Activate` | DefinitionId, ActivatedBy |
| `WorkflowStartedEvent` | `WorkflowInstance.Start` | InstanceId, DefinitionId, WorkflowName, StartedBy |
| `WorkflowPausedEvent` | `WorkflowInstance.Pause` | InstanceId, PausedBy |
| `WorkflowResumedEvent` | `WorkflowInstance.Resume` | InstanceId, ResumedBy |
| `WorkflowCompletedEvent` | `WorkflowInstance.CheckCompletion` | InstanceId, StartedBy |
| `WorkflowCancelledEvent` | `WorkflowInstance.Cancel` | InstanceId, CancelledBy |
| `WorkflowFailedEvent` | `WorkflowInstance.Fail` / `FailStep` | InstanceId, Reason, FailedBy |
| `StepAssignedEvent` | `WorkflowInstance.AssignStep` | StepId, AssigneeId, AssignedBy |
| `StepCompletedEvent` | `WorkflowInstance.CompleteStep` | StepId, CompletedBy |
| `StepFailedEvent` | `WorkflowInstance.FailStep` | StepId, Reason, FailedBy |
| `StepSkippedEvent` | `WorkflowInstance.SkipStep` | StepId, SkippedBy |

---

## What Comes Next

| Epic | Layer | What it adds |
|------|-------|-------------|
| EP-019 | Application | Commands, queries, MediatR handlers, FluentValidation, `Result<T>` wrapping |
| EP-020 | Infrastructure | EF Core entity configs, migrations, Dapper read models, domain event dispatch |
| EP-021 | API | REST endpoints, request/response DTOs |
| EP-022 | Frontend | Angular workflow management UI |
