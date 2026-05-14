# STRIDE Architecture

---

## Architecture Pattern

Modular Monolith with Clean Architecture per module.
Evolutionary — designed for future module extraction without breaking the host.

---

## Solution Structure

```
backend/src/
├── BuildingBlocks/
│   ├── STRIDE.BuildingBlocks.Domain/
│   ├── STRIDE.BuildingBlocks.Application/
│   └── STRIDE.BuildingBlocks.Infrastructure/
├── Modules/
│   ├── Identity/
│   │   ├── STRIDE.Modules.Identity.API/
│   │   ├── STRIDE.Modules.Identity.Application/
│   │   ├── STRIDE.Modules.Identity.Domain/
│   │   └── STRIDE.Modules.Identity.Infrastructure/
│   ├── Workflows/
│   │   ├── STRIDE.Modules.Workflows.API/
│   │   ├── STRIDE.Modules.Workflows.Application/
│   │   ├── STRIDE.Modules.Workflows.Domain/
│   │   └── STRIDE.Modules.Workflows.Infrastructure/
│   ├── Scheduling/
│   │   ├── STRIDE.Modules.Scheduling.API/
│   │   ├── STRIDE.Modules.Scheduling.Application/
│   │   ├── STRIDE.Modules.Scheduling.Domain/
│   │   └── STRIDE.Modules.Scheduling.Infrastructure/
│   ├── Reporting/
│   │   ├── STRIDE.Modules.Reporting.API/
│   │   ├── STRIDE.Modules.Reporting.Application/
│   │   ├── STRIDE.Modules.Reporting.Domain/
│   │   └── STRIDE.Modules.Reporting.Infrastructure/
│   ├── Notifications/
│   │   ├── STRIDE.Modules.Notifications.API/
│   │   ├── STRIDE.Modules.Notifications.Application/
│   │   ├── STRIDE.Modules.Notifications.Domain/
│   │   └── STRIDE.Modules.Notifications.Infrastructure/
│   ├── Invoicing/
│   │   ├── STRIDE.Modules.Invoicing.API/
│   │   ├── STRIDE.Modules.Invoicing.Application/
│   │   ├── STRIDE.Modules.Invoicing.Domain/
│   │   └── STRIDE.Modules.Invoicing.Infrastructure/
│   └── Administration/
│       ├── STRIDE.Modules.Administration.API/
│       ├── STRIDE.Modules.Administration.Application/
│       ├── STRIDE.Modules.Administration.Domain/
│       └── STRIDE.Modules.Administration.Infrastructure/
├── Host/
│   └── STRIDE.Host/
├── BFF/
│   └── STRIDE.BFF/
└── Gateway/
    └── STRIDE.Gateway/
```

---

## Layer Responsibilities

### Domain
- Entities, aggregates, value objects
- Repository interfaces
- Domain events (`IDomainEvent`)
- Domain services
- No external dependencies

### Application
- MediatR Commands, Queries, Handlers
- DTOs, response models
- FluentValidation validators
- References `IEventBus`, `ITenantContext` from BuildingBlocks
- Depends on Domain only

### Infrastructure
- EF Core `DbContext` (per module)
- Repository implementations
- External service adapters
- Module-specific migrations
- Depends on Domain + Application

### API
- ASP.NET Core Controllers
- Module registration extension (`AddXxxModule`)
- Request/response models
- Depends on Application only

---

## BuildingBlocks

### Domain
- `IDomainEvent`
- `IIntegrationEvent`
- `AuditableEntity` (Id, TenantId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
- `ValueObject`

### Application
- `IEventBus`
- `IBackgroundTaskQueue`
- `ITenantContext`
- `ICurrentUser`
- MediatR pipeline behaviors (logging, validation)

### Infrastructure
- `TenantMiddleware`
- `TenantContextProvider`
- `TenantResolver`
- Tenant-aware repository base
- Serilog configuration
- Redis client
- Correlation ID middleware
- Health check base

---

## STRIDE.Host

- ASP.NET Core entry point
- Registers all modules
- Configures middleware pipeline
- No business logic

---

## BFF Pattern Decision

`STRIDE.BFF` is a dedicated ASP.NET Core project.

Responsibilities:
- Session handling
- Auth orchestration
- HttpOnly cookie issuance
- Frontend request aggregation / proxying

Rules:
- Angular SPA calls ONLY the BFF
- BFF is the only surface that issues auth cookies
- BFF does NOT contain business logic
- BFF forwards authenticated requests to STRIDE.Host internal APIs

---

## MediatR Usage

Use for:
- Commands (write operations within a module)
- Queries (read operations within a module)
- Domain event dispatch (within a module)

Do NOT use for:
- Cross-module communication (use `IEventBus` instead)
- Full CQRS/event sourcing patterns (pragmatic use only)

---

## Queue Abstractions (Phase 1 — Interfaces Only)

Scaffolded in `BuildingBlocks.Application` and `BuildingBlocks.Domain`:

```
IDomainEvent
IIntegrationEvent
IEventBus
IBackgroundTaskQueue
```

No concrete implementations until Phase 3+.

---

## Multi-Tenant Strategy

Current:
- Shared database
- TenantId isolation on all tenant-aware tables

Future:
- Dedicated tenant databases
- Shard-aware routing
- High-volume tenant extraction

Architecture remains shard-aware and extraction-ready from day one.

---

## Data Access Strategy

### EF Core
- Transactional writes
- Aggregates
- Domain operations

### Dapper
- Reporting
- Dashboards
- High-performance reads

---

## Observability Strategy

Logging:
- Serilog
- Correlation IDs
- Tenant-aware logs (TenantId on every entry)

Monitoring:
- Application Insights
- OpenTelemetry
- Health endpoints (`/health`, `/health/live`, `/health/ready`, `/health/modules`)

---

## Infrastructure Strategy

- Azure-native deployment
- Docker-ready services (Docker Compose for local dev)
- CI/CD-ready (GitHub Actions skeleton)
- Future Kubernetes compatibility (stateless, health-check-ready)

---

## Future Extraction Candidates (Priority Order)

1. Notifications
2. Reporting
3. Invoicing
4. Audit

Each module is pre-positioned for extraction via:
- Independent DbContext
- No cross-module project references
- Integration events for cross-module communication
- Independent DI registration
