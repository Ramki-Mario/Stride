namespace STRIDE.Modules.Reporting.Application.ReadModels;

/// <summary>
/// Aggregated KPI snapshot for the dashboard — counts across all workflow states.
/// Sourced from workflows.WorkflowDefinitions and workflows.WorkflowInstances.
/// </summary>
public sealed record DashboardKpiDto(
    int TotalDefinitions,
    int ActiveDefinitions,
    int RunningInstances,
    int CompletedInstances,
    int FailedInstances,
    int CancelledInstances);
