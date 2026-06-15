using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Domain.Repositories;

public interface IKitReservationRepository
{
    Task<KitReservation?>              GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<KitReservation>> GetPendingByKitItemIdAsync(Guid kitItemId, CancellationToken ct = default);
    Task<KitReservation?>              GetOldestPendingByKitItemIdAsync(Guid kitItemId, CancellationToken ct = default);

    /// <summary>True if the user already holds a pending (active) request for this kit item.</summary>
    Task<bool>                         ExistsPendingForUserAsync(Guid kitItemId, Guid userId, CancellationToken ct = default);

    Task                               AddAsync(KitReservation reservation, CancellationToken ct = default);
    Task                               SaveChangesAsync(CancellationToken ct = default);
}
