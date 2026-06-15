using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Domain.Repositories;

public interface IKitCheckoutRepository
{
    Task<KitCheckout?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Atomically checks out a unit of <paramref name="item"/> if availability remains.
    /// Implementations MUST serialize concurrent checkouts of the same kit item so that
    /// the last available unit can never be issued twice (no oversubscription).
    /// Returns <c>true</c> when the checkout was persisted, <c>false</c> when no unit was available.
    /// </summary>
    Task<bool> TryCheckoutAsync(KitItem item, KitCheckout checkout, CancellationToken ct = default);

    /// <summary>Count of checkouts not yet returned (Active or Overdue) — units physically out.</summary>
    Task<int> GetOutstandingCountByKitItemIdAsync(Guid kitItemId, CancellationToken ct = default);

    /// <summary>Count of outstanding checkouts whose expected return date is before <paramref name="asOf"/>.</summary>
    Task<int> GetOverdueCountByKitItemIdAsync(Guid kitItemId, DateTime asOf, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
