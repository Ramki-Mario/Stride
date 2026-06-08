namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Dapper-based read service for lightweight workflow queries.
/// Used for list views and dashboard aggregations where full aggregate
/// hydration (via EF Core) would be unnecessarily expensive.
/// <para>
/// All queries are automatically scoped to the current tenant.
/// </para>
/// </summary>
public interface IWorkflowReadService
{
    /// <summary>
    /// Returns a summary list of workflow definitions for the current tenant,
    /// ordered by most recently updated first.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinitionReadModel>> GetWorkflowDefinitionSummariesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a summary list of workflow instances for the current tenant.
    /// Pass <paramref name="definitionId"/> to filter to history for a specific template.
    /// </summary>
    Task<IReadOnlyList<WorkflowInstanceReadModel>> GetWorkflowInstanceSummariesAsync(
        Guid tenantId,
        Guid? definitionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregate KPI counts for the dashboard for the current tenant.
    /// </summary>
    Task<WorkflowDashboardStats> GetDashboardStatsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all step instances assigned to <paramref name="userId"/> that are not
    /// yet in a terminal state (Completed / Skipped / Failed), within the current tenant.
    /// </summary>
    Task<IReadOnlyList<MyTaskReadModel>> GetMyTasksAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

// ── Read models (Dapper DTOs — flat, no navigation properties) ────────────────

public sealed record WorkflowDefinitionReadModel(
    Guid   Id,
    string Name,
    string? Description,
    string Status,
    int    StepCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record WorkflowInstanceReadModel(
    Guid   Id,
    Guid   WorkflowDefinitionId,
    string WorkflowName,
    string Status,
    int    TotalSteps,
    int    CompletedSteps,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record MyTaskReadModel(
    Guid     StepInstanceId,
    string   StepName,
    string   StepStatus,
    DateTime? AssignedAt,
    Guid     WorkflowInstanceId,
    string   WorkflowName,
    string   WorkflowStatus,
    string?  ClientName);

public sealed record WorkflowDashboardStats(
    int TotalDefinitions,
    int ActiveDefinitions,
    int TotalRunning,
    int TotalCompleted,
    int TotalFailed,
    int TotalCancelled);
