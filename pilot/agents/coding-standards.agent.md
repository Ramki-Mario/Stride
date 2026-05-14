# coding-standards.agent.md
# STRIDE — Coding Standards Agent

## Purpose
Persistent AI memory for naming, style, and structural conventions.
Read before generating any code to ensure consistency across the platform.

---

## C# Naming Conventions

```
Classes/Interfaces:  PascalCase        → WorkflowService, IWorkflowRepository
Methods:             PascalCase        → GetByIdAsync, CreateWorkflowAsync
Properties:          PascalCase        → TenantId, CreatedAt
Private fields:      _camelCase        → _context, _tenantContext
Parameters/Locals:   camelCase         → workflowId, cancellationToken
Constants:           PascalCase        → DefaultPageSize
Interfaces:          I-prefix          → IWorkflowRepository, ITenantContext
```

---

## File Naming (C#)

One class per file, filename matches class name exactly.

```
WorkflowController.cs
CreateWorkflowCommand.cs
CreateWorkflowCommandHandler.cs
GetWorkflowByIdQuery.cs
GetWorkflowByIdQueryHandler.cs
WorkflowCreatedDomainEvent.cs
WorkflowRepository.cs
WorkflowDbContext.cs
WorkflowModuleExtensions.cs
```

---

## TypeScript / Angular Naming

```
Components:     PascalCase + suffix   → WorkflowListComponent
Services:       PascalCase + suffix   → WorkflowService
Guards:         PascalCase + suffix   → AuthGuard
Interceptors:   PascalCase + suffix   → CorrelationInterceptor
Interfaces:     PascalCase, no I      → Workflow, WorkflowSummary
Enums:          PascalCase            → WorkflowStatus
Files:          kebab-case            → workflow-list.component.ts
Routes:         kebab-case            → /workflows/active
Signals:        camelCase             → workflowList, isLoading
```

---

## API Route Conventions

```
GET    /api/{module}/{resource}              → list
GET    /api/{module}/{resource}/{id}         → single item
POST   /api/{module}/{resource}              → create
PUT    /api/{module}/{resource}/{id}         → full update
PATCH  /api/{module}/{resource}/{id}         → partial update
DELETE /api/{module}/{resource}/{id}         → soft delete

Examples:
GET    /api/workflows/tasks
GET    /api/workflows/tasks/{id}
POST   /api/workflows/tasks
PATCH  /api/workflows/tasks/{id}/status
```

---

## Response Conventions

All API responses use a consistent envelope:

```json
{
  "data": { ... },
  "meta": {
    "correlationId": "...",
    "timestamp": "..."
  }
}
```

For paginated lists:
```json
{
  "data": [ ... ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "correlationId": "..."
  }
}
```

Errors follow RFC 7807 Problem Details:
```json
{
  "type": "https://stride.io/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "...",
  "correlationId": "..."
}
```

---

## Comment Policy

Write NO comments unless the WHY is non-obvious.
Never comment what the code obviously does.
Allowed: workaround explanations, hidden constraints, subtle invariants.

---

## Async Standards

- All I/O operations must be async (`async/await`)
- Always pass `CancellationToken` through the call stack
- Never use `.Result` or `.Wait()` — deadlock risk
- Suffix async methods with `Async`

---

## Validation

- Use FluentValidation for all command/query validation
- Register validators in Application DI registration
- Use MediatR pipeline behavior for automatic validation
- Return validation errors as Problem Details (400)

---

## Logging Standards

Log levels:
```
Debug    → detailed diagnostic (dev only)
Info     → significant business events (login, workflow created)
Warning  → recoverable issues (retry, fallback)
Error    → unrecoverable operation failure
Critical → platform-level failure
```

Every log entry from Application/Infrastructure must include:
```csharp
Log.ForContext("TenantId", tenantId)
   .ForContext("CorrelationId", correlationId)
   .ForContext("Module", "Workflows")
   .Information("Workflow {WorkflowId} created", workflowId);
```

---

## Git Commit Conventions

```
feat:     new feature
fix:      bug fix
refactor: code restructure without behavior change
chore:    tooling, config, build
docs:     documentation only
test:     tests only

Examples:
feat(identity): add tenant resolver middleware
fix(workflows): correct state transition guard
chore(infra): add docker-compose health checks
```
