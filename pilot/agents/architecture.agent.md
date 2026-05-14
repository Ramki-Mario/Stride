# architecture.agent.md
# STRIDE — Architecture Agent

## Purpose
Persistent AI architectural memory for the STRIDE platform.
Read this file before making any structural or cross-cutting decisions.

---

## Architecture Pattern

**Modular Monolith with Clean Architecture per module.**

STRIDE starts as a single deployable unit. Each module is designed for future extraction into an independent service without breaking the host.

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

## Layer Responsibilities (Per Module)

### Domain
- Entities, aggregates, value objects
- Repository interfaces (contracts only)
- Domain events (implement `IDomainEvent`)
- Domain services (pure business logic only)
- No dependencies on Application, Infrastructure, or external packages

### Application
- MediatR Commands, Queries, Handlers
- Application DTOs and response models
- Validation logic (FluentValidation)
- Application service orchestration
- Depends on Domain only
- References `IEventBus`, `IBackgroundTaskQueue` from BuildingBlocks

### Infrastructure
- EF Core `DbContext` per module (or shared with module schema)
- Repository implementations
- External service adapters (email, SMS, etc.)
- Module-specific migrations
- Depends on Domain and Application

### API
- ASP.NET Core Controllers
- Request/response models (not DTOs — kept at API boundary)
- `IServiceCollection` extension method for module registration
- Route prefix: `/api/{module}/...`
- Depends on Application only (never Infrastructure directly)

---

## BuildingBlocks Responsibilities

### STRIDE.BuildingBlocks.Domain
- `IDomainEvent` interface
- `IIntegrationEvent` interface
- `BaseEntity<TId>` base class
- `AuditableEntity` base class (TenantId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
- `ValueObject` base class

### STRIDE.BuildingBlocks.Application
- `IEventBus` interface
- `IBackgroundTaskQueue` interface
- `ITenantContext` interface
- `ICurrentUser` interface
- Base pipeline behaviors (logging, validation)

### STRIDE.BuildingBlocks.Infrastructure
- `TenantMiddleware`
- `TenantContextProvider`
- `TenantResolver`
- Base repository with TenantId filtering
- Serilog configuration
- Redis client setup
- Correlation ID middleware
- Health check base

---

## STRIDE.Host
- ASP.NET Core entry point
- Registers all modules via extension methods
- Configures middleware pipeline
- Configures routing and controllers
- Configures health checks
- No business logic

---

## STRIDE.BFF
- Dedicated project (not middleware inside Host)
- Responsibilities: session handling, auth orchestration, HttpOnly cookie issuance, frontend request aggregation
- Communicates with Identity module for auth
- Does NOT contain business logic
- Acts as the only surface the Angular frontend calls

---

## STRIDE.Gateway
- Future: API gateway / routing layer
- Currently: scaffold only
- Do NOT implement yet

---

## Cross-Cutting Rules

- Modules MUST NOT directly reference each other's projects
- Cross-module communication happens via `IEventBus` (integration events) or MediatR (internal)
- Every module registers itself via `AddXxxModule(this IServiceCollection services)` extension
- DbContext must NEVER appear in controllers or application layer
- All repositories must filter by TenantId automatically

---

## MediatR Usage

Use MediatR for:
- Commands (write operations)
- Queries (read operations within a module)
- Domain event dispatch within a module

Do NOT:
- Implement full CQRS/event sourcing
- Use MediatR for cross-module communication (use IEventBus instead)
- Over-abstract — keep handlers focused and direct

---

## Evolutionary Scalability

Future extraction candidates (in priority order):
1. Notifications
2. Reporting
3. Invoicing
4. Audit

Each module is pre-positioned for extraction because:
- Independent DbContext (or schema)
- No direct project references to other modules
- Integration events for cross-module communication
- Independent DI registration
