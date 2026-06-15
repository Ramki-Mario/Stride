using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Domain.Repositories;

public interface IKitCheckoutRepository
{
    Task<KitCheckout?>              GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int>                       GetActiveCountByKitItemIdAsync(Guid kitItemId, CancellationToken ct = default);
    Task<IReadOnlyList<KitCheckout>> GetActiveCheckoutsForKitItemAsync(Guid kitItemId, CancellationToken ct = default);
    Task                            AddAsync(KitCheckout checkout, CancellationToken ct = default);
    Task                            SaveChangesAsync(CancellationToken ct = default);
}
