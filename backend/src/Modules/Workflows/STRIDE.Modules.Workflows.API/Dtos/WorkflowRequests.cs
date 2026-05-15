namespace STRIDE.Modules.Workflows.API.Dtos;

// ─── Workflow Definition ───────────────────────────────────────────────────

/// <summary>
/// Body for POST /api/workflows  (create a new workflow definition).
/// </summary>
public sealed record CreateWorkflowRequest(
    string Name,
    string? Description,
    IReadOnlyList<StepRequestDto> Steps);

/// <summary>
/// Body for PUT /api/workflows/{id}  (update a Draft workflow definition).
/// </summary>
public sealed record UpdateWorkflowRequest(
    string Name,
    string? Description);

/// <summary>A single step included in a create-workflow request.</summary>
public sealed record StepRequestDto(
    string Name,
    string? Description,
    bool IsRequired = true);

// ─── Workflow Instance ─────────────────────────────────────────────────────

/// <summary>
/// Body for POST /api/workflows/{id}/start  (start a workflow instance).
/// Empty — the definition ID is taken from the route and startedBy from the JWT.
/// </summary>
public sealed record StartWorkflowRequest;
