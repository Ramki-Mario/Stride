using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowDefinition;

/// <summary>Full read model for a single workflow definition including its steps.</summary>
public sealed record WorkflowDefinitionDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    WorkflowStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid CreatedBy,
    decimal? SlaOffsetHours,
    IReadOnlyList<StepDefinitionDto> Steps);

public sealed record StepDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    int Order,
    bool IsRequired,
    Guid? RequiredRoleId,
    decimal? DueOffsetHours,
    IReadOnlyList<FieldDefinitionDto> Fields);

/// <summary>
/// Read model for a single data-capture field on a step definition.
/// FieldType is returned as its string name for easy consumption by Angular.
/// </summary>
public sealed record FieldDefinitionDto(
    Guid   Id,
    string Label,
    string FieldType,
    bool   IsRequired,
    int    DisplayOrder,
    string? HelpText,
    IReadOnlyList<string> DropdownOptions);
