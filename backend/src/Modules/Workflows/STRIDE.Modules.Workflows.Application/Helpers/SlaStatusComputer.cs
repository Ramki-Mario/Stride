using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Helpers;

/// <summary>
/// Stateless helper that computes the SLA status of a workflow instance from its timestamps.
/// Pure function — no side effects, no I/O.
/// </summary>
internal static class SlaStatusComputer
{
    /// <summary>
    /// Returns the SLA status, or <c>null</c> when no deadline was configured.
    /// </summary>
    /// <param name="createdAt">When the instance started (= CreatedAt).</param>
    /// <param name="deadlineAt">The absolute SLA deadline. Null = no SLA.</param>
    /// <param name="completedAt">When the instance finished, if it has. Null = still running.</param>
    /// <param name="now">The reference "current" time. Defaults to <see cref="DateTime.UtcNow"/>.</param>
    public static SlaStatus? Compute(
        DateTime createdAt,
        DateTime? deadlineAt,
        DateTime? completedAt,
        DateTime? now = null)
    {
        if (deadlineAt is null) return null;

        // For finished instances compare the actual end time against the deadline.
        var referenceTime = completedAt ?? (now ?? DateTime.UtcNow);

        if (referenceTime > deadlineAt.Value)
            return SlaStatus.Breached;

        // Completed before deadline — don't show AtRisk for already-finished work.
        if (completedAt.HasValue)
            return SlaStatus.OnTime;

        // Still running: warn when < 20 % of the window remains.
        var totalWindow  = deadlineAt.Value - createdAt;
        var timeRemaining = deadlineAt.Value - referenceTime;

        if (totalWindow.Ticks > 0 && timeRemaining / totalWindow < 0.20)
            return SlaStatus.AtRisk;

        return SlaStatus.OnTime;
    }
}
