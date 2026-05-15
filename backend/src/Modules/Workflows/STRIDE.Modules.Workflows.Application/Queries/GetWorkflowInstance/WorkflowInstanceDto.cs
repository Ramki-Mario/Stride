using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowInstance;

/// <summary>Full read model for a single workflow instance including its step instances.</summary>
public sealed record WorkflowInstanceDto(
    Guid Id,
    Guid TenantId,
    Guid WorkflowDefinitionId,
    string WorkflowName,
    WorkflowStatus Status,
    Guid StartedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<StepInstanceDto> Steps);

public sealed record StepInstanceDto(
    Guid Id,
    Guid StepDefinitionId,
    string StepName,
    int Order,
    bool IsRequired,
    StepStatus Status,
    Guid? AssigneeId,
    string? FailureReason,
    DateTime? CompletedAt);
