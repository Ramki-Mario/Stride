# Workflow Application Layer — KT Document

> **Stories:** EP-019 · US-044 · US-045 · US-046 · US-047 · US-048  
> **Layer:** Application (MediatR + FluentValidation, no EF Core or HTTP references)  
> **Namespace root:** `STRIDE.Modules.Workflows.Application`

---

## Directory Structure

```
STRIDE.Modules.Workflows.Application/
├── Abstractions/
│   ├── IWorkflowDefinitionRepository.cs
│   └── IWorkflowInstanceRepository.cs
├── Commands/
│   ├── CreateWorkflow/      (US-044) command + handler + validator + result
│   ├── UpdateWorkflow/      (US-044) command + handler + validator
│   ├── DeleteWorkflow/      (US-044) command + handler + validator
│   ├── ActivateWorkflow/    (US-045) command + handler + validator
│   ├── StartWorkflow/       (US-045) command + handler + validator + result
│   ├── PauseWorkflow/       (US-045) command + handler + validator
│   ├── ResumeWorkflow/      (US-045) command + handler + validator
│   ├── CancelWorkflow/      (US-045) command + handler + validator
│   ├── AssignStep/          (US-046) command + handler + validator
│   ├── CompleteStep/        (US-046) command + handler + validator
│   ├── FailStep/            (US-046) command + handler + validator
│   └── SkipStep/            (US-046) command + handler + validator
├── Queries/
│   ├── GetWorkflowDefinition/   (US-047) query + handler + validator + DTO
│   ├── ListWorkflowDefinitions/ (US-047) query + handler + summary DTO
│   ├── GetWorkflowInstance/     (US-047) query + handler + validator + DTO
│   └── ListWorkflowInstances/   (US-047) query + handler + summary DTO
└── WorkflowsApplicationExtensions.cs
```

---

## Architecture

### MediatR Pipeline

Every request flows through:

```
HTTP Request → Controller → IMediator.Send()
  └─► LoggingBehaviour<TRequest, TResponse>   (logs request name)
        └─► ValidationBehaviour<TRequest, TResponse>   (runs FluentValidation)
              └─► CommandHandler / QueryHandler   (domain logic)
```

Registered in `WorkflowsApplicationExtensions.AddWorkflowsApplication()`.

### Result Pattern

All handlers return `Result` or `Result<T>` — never throw for domain failures:

| Scenario | Return |
|---|---|
| Success with value | `Result.Success(value)` |
| Success, no value | `Result.Success()` |
| Domain rule violation | `Result.Failure("message")` |
| Not found | `Result.Failure("... not found")` |

The API layer (EP-021) maps `Result.IsFailure` to appropriate HTTP status codes.

---

## Command Reference

### US-044 — Workflow Definition CRUD

| Command | What it does | Guard |
|---|---|---|
| `CreateWorkflowCommand` | Creates a Draft definition + optional initial steps | Duplicate name check |
| `UpdateWorkflowCommand` | Updates name/description of a Draft | Domain: only Draft editable |
| `DeleteWorkflowCommand` | Soft-deletes a Draft | Domain: only Draft deletable |

### US-045 — Workflow Lifecycle

| Command | What it does | Guard |
|---|---|---|
| `ActivateWorkflowCommand` | Draft → Active | Domain: must have ≥1 step |
| `StartWorkflowCommand` | Active definition → Running instance (snapshots steps) | Domain: must be Active |
| `PauseWorkflowCommand` | Running → Paused | Domain: must be Running |
| `ResumeWorkflowCommand` | Paused → Running | Domain: must be Paused |
| `CancelWorkflowCommand` | Running/Paused → Cancelled | Domain: must be Running or Paused |

### US-046 — Step Operations

| Command | What it does | Guard |
|---|---|---|
| `AssignStepCommand` | Assigns a step to a user (Pending → Assigned) | Instance must be Running |
| `CompleteStepCommand` | Marks step Completed; auto-completes workflow if last step | Instance must be Running |
| `FailStepCommand` | Marks step Failed; cascades to workflow if step is required | Instance must be Running |
| `SkipStepCommand` | Skips an optional step; auto-completes workflow if last step | Instance must be Running; step must be optional |

---

## Query Reference (US-047)

| Query | Returns | Notes |
|---|---|---|
| `GetWorkflowDefinitionQuery(id)` | `WorkflowDefinitionDto` with full step list | Steps ordered by `Order` |
| `ListWorkflowDefinitionsQuery()` | `IReadOnlyList<WorkflowDefinitionSummaryDto>` | Ordered by UpdatedAt desc |
| `GetWorkflowInstanceQuery(id)` | `WorkflowInstanceDto` with full step list | Steps ordered by `Order` |
| `ListWorkflowInstancesQuery(definitionId?)` | `IReadOnlyList<WorkflowInstanceSummaryDto>` | Pass `definitionId` for history view; null for all instances |

### DTO Design

**Full DTOs** (Get* queries): include all fields including nested collections — used for detail pages.  
**Summary DTOs** (List* queries): lightweight, no nested collections — used for list/table views. `ListWorkflowInstancesQuery` includes a computed `CompletedSteps` count.

---

## Repository Abstractions

| Interface | Implementations | Notes |
|---|---|---|
| `IWorkflowDefinitionRepository` | EP-020 (EF Core + TenantAwareRepository) | `ExistsByNameAsync` for duplicate guard |
| `IWorkflowInstanceRepository` | EP-020 (EF Core + TenantAwareRepository) | `GetByDefinitionIdAsync` for history view; `GetByIdAsync` must eager-load `Steps` |

**Critical:** `IWorkflowInstanceRepository.GetByIdAsync` **must** eager-load step instances (EF Core `.Include(i => i.Steps)`). Without it, `WorkflowInstance.Steps` returns empty, breaking all step operations.

---

## Validation (US-048)

All commands and get-by-id queries have a `*Validator : AbstractValidator<T>`. Registered automatically via `AddValidatorsFromAssembly`. Rules:

- All `*Id` fields: `NotEmpty`
- `Name` fields: `NotEmpty` + `MaximumLength(200)`
- `Description` fields: `MaximumLength(1000)` when not null
- `Reason` (FailStep): `NotEmpty` + `MaximumLength(2000)`
- `Steps` list in CreateWorkflow: `RuleForEach` validates each step name/description

---

## What Comes Next

| Epic | Layer | What it adds |
|---|---|---|
| EP-020 | Infrastructure | EF Core entity configs, migrations for `workflows.*` schema, repository implementations, Dapper read models |
| EP-021 | API | `WorkflowsController` + `StepsController` dispatching to these commands/queries |
| EP-022 | Frontend | Angular workflow list page, detail page, create/edit form, step modals |
