namespace STRIDE.Modules.Reporting.Application.ReadModels;

/// <summary>
/// One row in a Workflow Summary report: per-definition instance counts broken down by state.
/// </summary>
public sealed record WorkflowSummaryReportRowDto(
    string WorkflowName,
    string Status,
    int TotalInstances,
    int RunningInstances,
    int CompletedInstances,
    int FailedInstances);
