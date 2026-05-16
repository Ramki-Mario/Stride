namespace STRIDE.Modules.Reporting.Application.ReadModels;

/// <summary>
/// Single data point in the workflow activity trend series.
/// Used to drive time-series charts on the dashboard.
/// </summary>
public sealed record WorkflowTrendDto(
    DateOnly TrendDate,
    int Started,
    int Completed,
    int Failed);
