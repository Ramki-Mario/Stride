using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowDefinitions;

/// <summary>
/// Lightweight summary used in workflow list views.
/// Does not include step details — use <c>GetWorkflowDefinitionQuery</c> for the full model.
/// </summary>
public sealed record WorkflowDefinitionSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    WorkflowStatus Status,
    int StepCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
