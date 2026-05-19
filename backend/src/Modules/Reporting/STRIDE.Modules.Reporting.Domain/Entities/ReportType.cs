namespace STRIDE.Modules.Reporting.Domain.Entities;

/// <summary>
/// Classifies the kind of data a generated report contains.
/// Stored as a string in the database (EF Core HasConversion).
/// </summary>
public enum ReportType
{
    /// <summary>Snapshot of dashboard KPIs (definition counts + instance state totals).</summary>
    DashboardKpi = 1,

    /// <summary>Daily workflow activity trend series (started / completed / failed per day).</summary>
    WorkflowTrend = 2,

    /// <summary>Per-definition summary: instance counts broken down by state.</summary>
    WorkflowSummary = 3
}
