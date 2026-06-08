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
    IReadOnlyList<StepDefinitionDto> Steps);

public sealed record StepDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    int Order,
    bool IsRequired,
    Guid? RequiredRoleId);
