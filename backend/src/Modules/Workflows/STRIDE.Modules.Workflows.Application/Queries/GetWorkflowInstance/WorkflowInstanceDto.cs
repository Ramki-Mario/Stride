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
    IReadOnlyList<StepInstanceDto> Steps)
{
    /// <summary>Sum of all billable line totals across every step in this instance.</summary>
    public decimal BillableTotal => Steps.Sum(s => s.BillableSubtotal);
}

public sealed record StepInstanceDto(
    Guid Id,
    Guid StepDefinitionId,
    string StepName,
    int Order,
    bool IsRequired,
    StepStatus Status,
    Guid? AssigneeId,
    DateTime? AssignedAt,
    Guid? RequiredRoleId,
    string? FailureReason,
    DateTime? CompletedAt,
    IReadOnlyList<BillableItemDto> BillableItems)
{
    /// <summary>Sum of line totals (Quantity × UnitPrice) for this step.</summary>
    public decimal BillableSubtotal => BillableItems.Sum(b => b.LineTotal);
}

public sealed record BillableItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string Unit,
    decimal LineTotal);
