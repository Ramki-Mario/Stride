# Workflow API Layer — KT Document

> **Stories:** EP-021 · US-052 · US-053  
> **Layer:** API (controllers, request DTOs — no domain or infrastructure references)  
> **Namespace root:** `STRIDE.Modules.Workflows.API`

---

## Directory Structure

```
STRIDE.Modules.Workflows.API/
├── Controllers/
│   ├── WorkflowsHealthController.cs    Pre-existing health stub
│   ├── WorkflowsController.cs          Definitions CRUD + Instance lifecycle
│   └── StepsController.cs              Step operations (assign/complete/fail/skip)
├── Dtos/
│   ├── WorkflowRequests.cs             Create/Update/Start request bodies
│   └── StepRequests.cs                 Assign/Fail request bodies
├── Extensions/
│   └── WorkflowsModuleExtensions.cs    DI wiring (Application + Infrastructure)
└── api.readme.md                       This file
```

---

## Route Map

### WorkflowsController — `api/workflows`

All routes require a valid JWT Bearer token (`[Authorize]`).

| Method | Route | Command / Query | Description |
|--------|-------|-----------------|-------------|
| GET | `/api/workflows` | `ListWorkflowDefinitionsQuery` | List all definitions (tenant-scoped) |
| POST | `/api/workflows` | `CreateWorkflowCommand` | Create Draft definition with optional initial steps |
| GET | `/api/workflows/{id}` | `GetWorkflowDefinitionQuery` | Get definition + step definitions |
| PUT | `/api/workflows/{id}` | `UpdateWorkflowCommand` | Update name/description (Draft only) |
| DELETE | `/api/workflows/{id}` | `DeleteWorkflowCommand` | Soft-delete definition |
| POST | `/api/workflows/{id}/activate` | `ActivateWorkflowCommand` | Draft → Active |
| POST | `/api/workflows/{id}/start` | `StartWorkflowCommand` | Start new instance from Active definition |
| GET | `/api/workflows/{id}/instances` | `ListWorkflowInstancesQuery` | List instances for a definition |
| GET | `/api/workflows/instances/{iid}` | `GetWorkflowInstanceQuery` | Get instance + step instances |
| POST | `/api/workflows/instances/{iid}/pause` | `PauseWorkflowCommand` | Running → Paused |
| POST | `/api/workflows/instances/{iid}/resume` | `ResumeWorkflowCommand` | Paused → Running |
| POST | `/api/workflows/instances/{iid}/cancel` | `CancelWorkflowCommand` | Cancel a Running/Paused instance |

### StepsController — `api/workflows/instances/{instanceId}/steps`

All routes require a valid JWT Bearer token (`[Authorize]`).

| Method | Route | Command | Description |
|--------|-------|---------|-------------|
| POST | `/{stepId}/assign` | `AssignStepCommand` | Assign step to a user |
| POST | `/{stepId}/complete` | `CompleteStepCommand` | Mark step completed (auto-checks workflow completion) |
| POST | `/{stepId}/fail` | `FailStepCommand` | Mark step failed (required step cascades to workflow failure) |
| POST | `/{stepId}/skip` | `SkipStepCommand` | Skip step (auto-checks workflow completion) |

---

## Request / Response DTOs

### Request bodies (in `Dtos/`)

| DTO | Used by | Fields |
|-----|---------|--------|
| `CreateWorkflowRequest` | POST /api/workflows | `Name`, `Description?`, `Steps: StepRequestDto[]` |
| `StepRequestDto` | CreateWorkflowRequest | `Name`, `Description?`, `IsRequired` (default: true) |
| `UpdateWorkflowRequest` | PUT /api/workflows/{id} | `Name`, `Description?` |
| `AssignStepRequest` | POST .../assign | `AssigneeId: Guid` |
| `FailStepRequest` | POST .../fail | `Reason: string` |

### Response bodies (defined in Application layer)

| DTO | Used by | Where defined |
|-----|---------|---------------|
| `WorkflowDefinitionSummaryDto` | GET list | `Queries/ListWorkflowDefinitions/` |
| `WorkflowDefinitionDto` | GET single | `Queries/GetWorkflowDefinition/` |
| `CreateWorkflowResult` | POST create | `Commands/CreateWorkflow/` |
| `StartWorkflowResult` | POST start | `Commands/StartWorkflow/` |
| `WorkflowInstanceSummaryDto` | GET instance list | `Queries/ListWorkflowInstances/` |
| `WorkflowInstanceDto` | GET instance | `Queries/GetWorkflowInstance/` |

---

## Error Mapping

Controllers use a consistent pattern for mapping `Result` failures to HTTP status codes:

| Condition in `result.Error` | HTTP Status |
|----------------------------|-------------|
| Contains `"not found"` | 404 Not Found |
| Contains `"already exists"` | 409 Conflict |
| Any other failure | 400 Bad Request |

This mirrors the Identity module's controller pattern for consistency across the API surface.

---

## Dependencies Injected per Controller

### WorkflowsController
- `IMediator` — dispatches all commands and queries
- `ICurrentUser` — provides `UserId` for audit (CreatedBy, ActivatedBy, etc.)
- `ITenantContext` — provides `TenantId` for `CreateWorkflowCommand` (note: `ICurrentUser` does not expose TenantId)

### StepsController
- `IMediator` — dispatches step commands
- `ICurrentUser` — provides `UserId` as the acting user (AssignedBy, CompletedBy, etc.)

---

## Authorization

All endpoints are protected with `[Authorize]` at the controller level. No fine-grained RBAC policies are applied at this layer yet — that is scoped to Phase 6 (SaaS Readiness). The JWT Bearer token is validated by `STRIDE.Host` middleware (registered in `Program.cs`).

---

## What Comes Next

| Epic | What it adds |
|------|-------------|
| EP-022 | Angular UI: workflow list page, detail page, create/edit form, step modals |
| Phase 6 | RBAC policies on workflow mutations (e.g. only Managers can activate/delete) |
