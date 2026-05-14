# api.agent.md
# STRIDE — API Agent

## Purpose
Persistent AI memory for API design, controller patterns, and BFF aggregation.
Read before implementing any controller, endpoint, or BFF route.

---

## Controller Pattern

```csharp
[ApiController]
[Route("api/workflows/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWorkflowByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,OperationsManager")]
    public async Task<IActionResult> Create(CreateWorkflowRequest request, CancellationToken ct)
    {
        var command = new CreateWorkflowCommand(request.Title, request.Type);
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(result.Error);
    }
}
```

Rules:
- Inject only `IMediator` — never repositories, DbContext, or domain services
- Always pass `CancellationToken`
- Use `[Authorize]` with role/policy attributes
- Return consistent response types (see coding-standards)
- Keep action methods under 10 lines — all logic in handlers

---

## BFF Route Design

BFF exposes routes that the Angular frontend calls:

```
POST   /bff/auth/login
POST   /bff/auth/logout
GET    /bff/auth/me
POST   /bff/auth/refresh

GET    /bff/workflows
GET    /bff/workflows/{id}
POST   /bff/workflows
```

BFF routes:
- Validate session cookie
- Forward to appropriate backend module endpoint
- May aggregate multiple backend calls into a single frontend response
- May reshape responses for frontend consumption
- Must NOT contain business rules

---

## BFF Aggregation Pattern

```csharp
// BFF Dashboard aggregation example
[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboard(CancellationToken ct)
{
    // Parallel calls to internal APIs
    var (workflowSummary, pendingApprovals, scheduleToday) = await (
        _workflowClient.GetSummaryAsync(ct),
        _workflowClient.GetPendingApprovalsAsync(ct),
        _schedulingClient.GetTodayAsync(ct)
    );

    return Ok(new DashboardResponse(workflowSummary, pendingApprovals, scheduleToday));
}
```

---

## API Versioning

Phase 1: No versioning (MVP, single version).
Phase 2+: Use URL versioning `/api/v1/...` when breaking changes are needed.
Do not implement versioning infrastructure until it is actually needed.

---

## Pagination Convention

All list endpoints must support pagination:

```
GET /api/workflows/tasks?page=1&pageSize=20&sortBy=createdAt&sortDir=desc
```

Response meta includes: `page`, `pageSize`, `totalCount`, `totalPages`.

Default page size: 20. Maximum page size: 100.

---

## Filtering Convention

Filters passed as query parameters:
```
GET /api/workflows/tasks?status=InProgress&assignedTo={userId}
GET /api/workflows/tasks?from=2024-01-01&to=2024-01-31
```

Never accept raw SQL fragments or dynamic LINQ from query params.
Map query params to typed filter objects in the Application layer.

---

## Health Check Endpoints

```
GET /health           → overall platform health
GET /health/live      → liveness (is process alive)
GET /health/ready     → readiness (dependencies ready: DB, Redis)
GET /health/modules   → per-module health status
```

Module health response:
```json
{
  "modules": [
    { "name": "Workflows",     "status": "Healthy" },
    { "name": "Notifications", "status": "Degraded" },
    { "name": "Invoicing",     "status": "Offline" }
  ]
}
```

---

## CORS Policy

- BFF: allow Angular SPA origin only
- Backend modules: no public CORS (called only by BFF and internal services)
- Never use wildcard CORS (`*`) in any environment

---

## AI Constraints

MUST:
- Keep controllers thin (dispatch to MediatR only)
- Return consistent response envelopes
- Apply `[Authorize]` to all non-public endpoints
- Pass CancellationToken through every action

MUST NOT:
- Inject DbContext or repositories into controllers
- Put business logic in controllers
- Return raw entity objects from endpoints (use DTOs/response models)
- Accept untyped/dynamic request bodies
