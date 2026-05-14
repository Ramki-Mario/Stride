# database.agent.md
# STRIDE — Database Agent

## Purpose
Persistent AI memory for data access decisions.
Read before implementing any entity, repository, migration, or query.

---

## Database Platform

- Azure SQL (SQL Server)
- Shared database, TenantId isolation
- Shard-aware design for future extraction

---

## ORM Split Strategy

### EF Core — for:
- Transactional writes
- Domain aggregate operations
- Relationship persistence within a module
- State transitions with audit logging
- Migrations

### Dapper — for:
- Reporting queries
- Dashboard read projections
- Multi-table aggregated reads
- KPI metrics
- High-performance read paths

Never mix EF Core and Dapper for the same operation.
Use EF Core for writes and domain reads.
Use Dapper for reporting reads.

---

## Mandatory Table Columns

Every tenant-aware table MUST include:

```sql
Id          UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()
TenantId    UNIQUEIDENTIFIER  NOT NULL
CreatedAt   DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
UpdatedAt   DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
CreatedBy   UNIQUEIDENTIFIER  NOT NULL
IsDeleted   BIT               NOT NULL  DEFAULT 0
```

Soft-delete pattern: never hard-delete tenant-owned records.
All queries must filter `IsDeleted = 0` automatically.

---

## Base Entity (BuildingBlocks.Domain)

```csharp
public abstract class AuditableEntity
{
    public Guid Id        { get; protected set; }
    public Guid TenantId  { get; protected set; }
    public DateTime CreatedAt  { get; protected set; }
    public DateTime UpdatedAt  { get; protected set; }
    public Guid CreatedBy      { get; protected set; }
    public bool IsDeleted      { get; protected set; }
}
```

---

## Indexing Strategy

Tenant-aware tables must use TenantId-first composite indexes:

```sql
CREATE INDEX IX_{Table}_TenantId_Status    ON {Table} (TenantId, Status)
CREATE INDEX IX_{Table}_TenantId_CreatedAt ON {Table} (TenantId, CreatedAt DESC)
```

Primary key: `Id` (UNIQUEIDENTIFIER, NEWSEQUENTIALID for insert performance).
Foreign keys: always include TenantId in composite FKs where relevant.

---

## Repository Base Pattern

```csharp
public abstract class TenantAwareRepository<T> where T : AuditableEntity
{
    protected readonly DbContext _context;
    protected readonly ITenantContext _tenant;

    protected IQueryable<T> Query =>
        _context.Set<T>()
                .Where(x => x.TenantId == _tenant.TenantId && !x.IsDeleted);
}
```

All repositories MUST derive from this or equivalent.
Never write a query without TenantId filtering.

---

## Redis Strategy

Session storage:
```
tenant:{tenantId}:session:{sessionId}
```

Auth cache:
```
tenant:{tenantId}:user:{userId}:auth
```

Workflow cache (future):
```
tenant:{tenantId}:workflow:{workflowId}
```

Rules:
- All Redis keys must be tenant-prefixed
- TTL must be set on all keys
- Never share a key between tenants
- Never use a global (non-tenant-prefixed) cache for tenant data

---

## Migration Strategy

- Each module has independent migrations
- Use EF Core `--project` flag per module
- Migration naming: `{Timestamp}_{Description}` (auto-generated)
- Never manually edit generated migration files
- Run migrations at startup in dev; use migration bundles in prod

---

## DbContext Per Module + SQL Schema Separation

Each module has its own `DbContext` **and its own SQL schema**:

| Module | SQL Schema | DbContext |
|---|---|---|
| Identity | `identity` | `IdentityDbContext` |
| Workflows | `workflows` | `WorkflowDbContext` |
| Scheduling | `scheduling` | `SchedulingDbContext` |
| Reporting | `reporting` | `ReportingDbContext` |
| Notifications | `notifications` | `NotificationsDbContext` |
| Invoicing | `invoicing` | `InvoicingDbContext` |
| Administration | `administration` | `AdministrationDbContext` |

Schema separation means tables are `identity.Users`, `workflows.Tasks`, `scheduling.Assignments`, etc.

Configure schema in EF Core `OnModelCreating`:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("identity");
    // or per entity: modelBuilder.Entity<User>().ToTable("Users", "identity");
}
```

DbContexts are NOT shared between modules.
Cross-module reads via Dapper against the shared DB are acceptable for reporting only.
Migrations are per-module and per-schema — never run migrations across modules in a single command.

---

## Data Access Anti-Patterns (Never Do)

- Never expose `IQueryable` outside of a repository
- Never inject `DbContext` into a controller or application service
- Never query without TenantId filter
- Never hard-delete tenant records
- Never write a global cache key without tenant prefix
- Never share a DbContext between modules
- Never skip soft-delete filter in queries
