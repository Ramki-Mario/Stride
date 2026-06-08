using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.API.Dtos;

// ─── Workflow Definition ───────────────────────────────────────────────────

/// <summary>
/// Body for POST /api/workflows  (create a new workflow definition).
/// </summary>
public sealed record CreateWorkflowRequest(
    string Name,
    string? Description,
    IReadOnlyList<StepRequestDto> Steps,
    decimal? SlaOffsetHours = null);

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
    bool IsRequired = true,
    Guid? RequiredRoleId = null,
    IReadOnlyList<FieldDefinitionRequestDto>? FieldDefinitions = null,
    decimal? DueOffsetHours = null);

/// <summary>
/// A single data-capture field on a step — submitted in the create-workflow request.
/// </summary>
public sealed record FieldDefinitionRequestDto(
    string Label,
    StepFieldType FieldType,
    bool IsRequired,
    string? HelpText = null,
    IReadOnlyList<string>? DropdownOptions = null);
