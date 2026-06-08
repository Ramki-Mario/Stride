namespace STRIDE.Modules.Workflows.Domain.Enums;

/// <summary>
/// Whether a workflow instance is on track against its SLA deadline.
/// Computed from DeadlineAt vs the current time (or CompletedAt for finished instances).
/// </summary>
public enum SlaStatus
{
    /// <summary>Sufficient time remains — more than 20 % of the SLA window left.</summary>
    OnTime = 0,

    /// <summary>Less than 20 % of the SLA window remains; urgent action needed.</summary>
    AtRisk = 1,

    /// <summary>The deadline has passed (or the workflow completed after the deadline).</summary>
    Breached = 2,
}
