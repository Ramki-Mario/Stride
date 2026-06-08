using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowInstances;

/// <summary>
/// Lightweight summary used in instance list and history views.
/// Does not include step details — use <c>GetWorkflowInstanceQuery</c> for the full model.
/// </summary>
public sealed record WorkflowInstanceSummaryDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    string WorkflowName,
    WorkflowStatus Status,
    int TotalSteps,
    int CompletedSteps,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? DeadlineAt,
    SlaStatus? SlaStatus);
