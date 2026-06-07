namespace STRIDE.Modules.Workflows.Domain.Enums;

/// <summary>
/// The unit of measure for a billable item logged against a workflow step.
/// </summary>
public enum BillableUnit
{
    /// <summary>Time billed by the hour.</summary>
    Hours = 0,

    /// <summary>Discrete items (parts, licences, deliverables).</summary>
    Each = 1,

    /// <summary>Time billed by the day.</summary>
    Day = 2,

    /// <summary>Fixed-price charge (flat fee, call-out charge, etc.).</summary>
    Fixed = 3,
}
