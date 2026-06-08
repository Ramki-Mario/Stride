namespace STRIDE.Modules.Workflows.Application.Queries.GetMyTasks;

/// <summary>
/// Lightweight DTO returned by the My Tasks query — one row per step instance
/// assigned to the requesting user that has not yet reached a terminal state.
/// </summary>
public sealed record MyTaskDto(
    Guid      StepInstanceId,
    string    StepName,
    string    StepStatus,
    DateTime? AssignedAt,
    Guid      WorkflowInstanceId,
    string    WorkflowName,
    string    WorkflowStatus,
    string?   ClientName);
