# backend.agent.md
# STRIDE — Backend Agent

## Purpose
Persistent AI memory for backend implementation decisions.
Read before implementing any backend layer, module, or service.

---

## Project Naming Convention

```
STRIDE.BuildingBlocks.Domain
STRIDE.BuildingBlocks.Application
STRIDE.BuildingBlocks.Infrastructure

STRIDE.Modules.{ModuleName}.Domain
STRIDE.Modules.{ModuleName}.Application
STRIDE.Modules.{ModuleName}.Infrastructure
STRIDE.Modules.{ModuleName}.API

STRIDE.Host
STRIDE.BFF
STRIDE.Gateway
```

---

## Module Registration Pattern

Each module exposes a single extension method on `IServiceCollection`:

```csharp
// Inside STRIDE.Modules.Identity.API
public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        return services;
    }
}
```

`STRIDE.Host` calls these in `Program.cs`:
```csharp
builder.Services.AddIdentityModule(config);
builder.Services.AddWorkflowsModule(config);
// etc.
```

---

## Layer Dependency Rules

```
API         → Application only
Application → Domain only
Infrastructure → Domain + Application (for interface implementations)
Domain      → nothing (pure)
BuildingBlocks.Application → BuildingBlocks.Domain
```

Never reference Infrastructure from API or Application layers.
Never reference one Module's projects from another Module's projects.

---

## Data Access Split

### EF Core — used for:
- Transactional writes
- Domain aggregate persistence
- State transitions
- Audit records
- Relationship navigation within a module

### Dapper — used for:
- Reporting queries
- Dashboard projections
- Read models / view queries
- High-performance multi-table reads
- Aggregated metrics

Each module gets its own `DbContext`.
Migrations are per-module, not shared.

---

## Repository Pattern

Every repository must:
1. Accept `ITenantContext` via constructor injection
2. Automatically apply `.Where(x => x.TenantId == _tenantContext.TenantId)` on every query
3. Never expose raw `IQueryable` outside the repository
4. Use the repository interface from Domain layer

```csharp
// Domain layer defines the contract
public interface IWorkflowRepository
{
    Task<Workflow?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Workflow workflow, CancellationToken ct);
}

// Infrastructure layer implements it
public class WorkflowRepository : IWorkflowRepository
{
    private readonly WorkflowDbContext _context;
    private readonly ITenantContext _tenant;
    // ...
}
```

---

## Controller Standards

- Use `[ApiController]` and `[Route("api/[module]/[controller]")]`
- Return `IActionResult` or typed `ActionResult<T>`
- Inject MediatR `IMediator`, never repositories or DbContext
- Keep controllers thin — dispatch to MediatR, return result
- Use `[Authorize]` with role/policy attributes
- Always include `CancellationToken` in action parameters

---

## MediatR Standards

Commands:
- Suffix with `Command`: `CreateWorkflowCommand`
- Handlers suffix with `Handler`: `CreateWorkflowCommandHandler`
- Return `Result<T>` or typed response record

Queries:
- Suffix with `Query`: `GetWorkflowByIdQuery`
- Handlers suffix with `Handler`: `GetWorkflowByIdQueryHandler`

Domain Events:
- Suffix with `DomainEvent`: `WorkflowCreatedDomainEvent`
- Implement `IDomainEvent`
- Dispatched within the module, not cross-module

Integration Events:
- Suffix with `IntegrationEvent`: `WorkflowCompletedIntegrationEvent`
- Implement `IIntegrationEvent`
- Published via `IEventBus` for cross-module communication

---

## Result<T> Pattern

All Application layer command and query handlers return `Result<T>` or `Result`.
This is a **custom type** defined in `STRIDE.BuildingBlocks.Application` (or a `Shared` sub-project).
Do NOT use external NuGet packages (FluentResults, ErrorOr, OneOf, etc.).

Minimum shape:
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}
```

---

## BFF ↔ Host Communication

STRIDE.BFF communicates with STRIDE.Host via **typed HttpClient clients**.
Each backend surface the BFF needs is wrapped in a named typed client:

```csharp
// Registered in BFF DI
services.AddHttpClient<IdentityApiClient>(client =>
    client.BaseAddress = new Uri(config["Services:Host"]));

services.AddHttpClient<WorkflowApiClient>(client =>
    client.BaseAddress = new Uri(config["Services:Host"]));
```

Rules:
- BFF controllers inject typed clients, never raw `HttpClient`
- YARP is intentionally deferred — do NOT introduce it yet
- All typed clients attach the session-resolved bearer token (or forward cookies) internally

---

## Queue Abstractions — Interfaces + In-Process IEventBus

Interfaces defined in BuildingBlocks:

```csharp
// BuildingBlocks.Domain
public interface IDomainEvent { }
public interface IIntegrationEvent { }

// BuildingBlocks.Application
public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : IIntegrationEvent;
}

public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct);
}
```

**IEventBus has an in-process implementation** (Phase 1–3):

```csharp
// BuildingBlocks.Infrastructure
internal sealed class MediatREventBus : IEventBus
{
    private readonly IPublisher _publisher;
    public MediatREventBus(IPublisher publisher) => _publisher = publisher;

    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent
        => _publisher.Publish(integrationEvent, ct);
}
```

`IIntegrationEvent` must also implement `MediatR.INotification` for this to work.
Azure Service Bus implementation of `IEventBus` is deferred to Phase 6+.
`IBackgroundTaskQueue` remains interface-only until Phase 3+.

---

## Error Handling

- Use a `Result<T>` pattern for Application layer operations (not exceptions for expected failures)
- Use global exception middleware in Host for unhandled exceptions
- Log all exceptions with TenantId, CorrelationId, RequestId
- Return RFC 7807 Problem Details from controllers

---

## Configuration Strategy

- Use `appsettings.json` + `appsettings.{Environment}.json`
- Secrets via environment variables (never committed)
- Use strongly-typed options classes with `IOptions<T>`
- Each module can define its own options class

---

## Logging Standards

All log entries must include:
- `TenantId`
- `CorrelationId`
- `RequestId`
- `UserId` (when authenticated)
- Module name as a structured property

Use Serilog enrichers to attach these automatically via middleware.
Never use `Console.WriteLine` or `Debug.WriteLine`.
