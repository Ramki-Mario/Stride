# Workflow Infrastructure Layer — KT Document

> **Stories:** EP-020 · US-049 · US-050 · US-051  
> **Layer:** Infrastructure (EF Core, Dapper, SQL Server — no HTTP references)  
> **Namespace root:** `STRIDE.Modules.Workflows.Infrastructure`

---

## Directory Structure

```
STRIDE.Modules.Workflows.Infrastructure/
├── Persistence/
│   ├── WorkflowsDbContext.cs            EF Core DbContext with domain event dispatch
│   ├── WorkflowsDbContextFactory.cs     Design-time factory for dotnet-ef tooling
│   ├── Configurations/
│   │   ├── WorkflowDefinitionConfiguration.cs
│   │   ├── StepDefinitionConfiguration.cs
│   │   ├── WorkflowInstanceConfiguration.cs
│   │   └── StepInstanceConfiguration.cs
│   └── Repositories/
│       ├── WorkflowDefinitionRepository.cs
│       └── WorkflowInstanceRepository.cs
├── ReadModels/
│   └── WorkflowReadService.cs           Dapper-based read service
├── Migrations/
│   └── *_InitialCreate.cs               workflows.* schema (applied to STRIDE DB)
└── WorkflowsInfrastructureExtensions.cs
```

---

## Database Schema

Schema name: **`workflows`**  
Migration history table: `workflows.__EFMigrationsHistory`

### Tables

| Table | Description |
|---|---|
| `workflows.WorkflowDefinitions` | Workflow templates (Draft/Active/Archived) |
| `workflows.StepDefinitions` | Ordered steps within a definition (cascades on definition delete) |
| `workflows.WorkflowInstances` | Live executions of a definition |
| `workflows.StepInstances` | Per-step snapshots within an instance (cascades on instance delete) |

### Key Indexes

| Table | Index | Purpose |
|---|---|---|
| `WorkflowDefinitions` | `(TenantId, Name)` UNIQUE WHERE IsDeleted=0 | Duplicate name guard |
| `WorkflowDefinitions` | `(TenantId, Status)` | Filter by status in list views |
| `WorkflowInstances` | `(TenantId, WorkflowDefinitionId)` | History view for a definition |
| `WorkflowInstances` | `(TenantId, Status)` | Dashboard KPI queries |
| `StepInstances` | `(WorkflowInstanceId, Order)` | Ordered step display |
| `StepInstances` | `AssigneeId` | "My assigned steps" view (Phase 6+) |

### Enum Storage

`WorkflowStatus` and `StepStatus` are stored as `nvarchar(20)` strings (not integers) for readability in queries. EF Core `HasConversion<string>()` handles serialization.

---

## Domain Event Dispatch (US-049)

`WorkflowsDbContext.SaveChangesAsync` implements the post-commit dispatch pattern:

```
1. Collect domain events from all tracked AuditableEntity instances
2. Clear events from all aggregates (before saving — prevents double-dispatch on retry)
3. await base.SaveChangesAsync()  ← DB transaction commits
4. Publish each event via IPublisher wrapped in DomainEventNotification<T>
```

`DomainEventNotification<T>` lives in `BuildingBlocks.Infrastructure.Events` and wraps any `IDomainEvent` as an `INotification` for MediatR dispatch.

**To handle a domain event:** create a class implementing `INotificationHandler<DomainEventNotification<WorkflowStartedEvent>>` in any module's Application layer. MediatR auto-discovers it.

---

## Repository Pattern (US-050)

Both repositories extend `TenantAwareRepository<TEntity, WorkflowsDbContext>` from BuildingBlocks, which automatically applies `TenantId + IsDeleted = 0` filters to the base `Query` property.

### Critical: StepInstance Eager Loading

`WorkflowInstanceRepository.GetByIdAsync` **always** includes Steps:
```csharp
.Include(i => i.Steps)
```
This is mandatory — without it, `WorkflowInstance.Steps` returns empty and all step operations fail silently.

### Navigation / Backing Field

`WorkflowDefinition.Steps` and `WorkflowInstance.Steps` are exposed as `IReadOnlyList<T>` backed by a private `List<T>` field named `_steps`. EF Core configuration:
```csharp
builder.Navigation(d => d.Steps)
    .HasField("_steps")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

---

## Dapper Read Service (US-051)

`WorkflowReadService` implements `IWorkflowReadService` (defined in Application/Abstractions) using raw SQL via Dapper + `Microsoft.Data.SqlClient`. Used for:

| Method | Use case |
|---|---|
| `GetWorkflowDefinitionSummariesAsync` | Workflow list page — includes step COUNT in SQL |
| `GetWorkflowInstanceSummariesAsync` | Instance list + history view; optional definitionId filter |
| `GetDashboardStatsAsync` | Dashboard KPI widget (total/active/running/completed/failed/cancelled) |

**Why Dapper for reads?** EF Core includes full aggregate graph (including steps) on every query. Dapper reads flat projections with SQL JOINs — faster and more appropriate for list/reporting views where step-level detail is not needed (ADR-005).

---

## Running Migrations

```bash
# Add a new migration
dotnet ef migrations add <Name> \
  --project src/Modules/Workflows/STRIDE.Modules.Workflows.Infrastructure/STRIDE.Modules.Workflows.Infrastructure.csproj \
  --output-dir Migrations

# Apply to database
dotnet ef database update \
  --project src/Modules/Workflows/STRIDE.Modules.Workflows.Infrastructure/STRIDE.Modules.Workflows.Infrastructure.csproj
```

Connection string used at design time: local SQL Express `LAPTOP-417EMKN1\SQLEXPRESS` (hard-coded in `WorkflowsDbContextFactory` — not committed with secrets).

---

## DI Registration

`WorkflowsInfrastructureExtensions.AddWorkflowsInfrastructure(services, configuration)`:

| Registration | Lifetime | Interface |
|---|---|---|
| `WorkflowsDbContext` | Scoped | — |
| `WorkflowDefinitionRepository` | Scoped | `IWorkflowDefinitionRepository` |
| `WorkflowInstanceRepository` | Scoped | `IWorkflowInstanceRepository` |
| `WorkflowReadService` | Scoped | `IWorkflowReadService` |

Called from `WorkflowsModuleExtensions` in the API project (wired in EP-021).

---

## What Comes Next

| Epic | Layer | What it adds |
|---|---|---|
| EP-021 | API | `WorkflowsController` + `StepsController` dispatching MediatR commands/queries |
| EP-022 | Frontend | Angular workflow list, detail, create/edit, step modals |
