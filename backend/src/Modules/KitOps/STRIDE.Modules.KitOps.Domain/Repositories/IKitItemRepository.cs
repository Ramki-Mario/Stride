using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Domain.Repositories;

public interface IKitItemRepository
{
    Task<KitItem?>  GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool>      ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
    Task            AddAsync(KitItem item, CancellationToken ct = default);
    Task            SaveChangesAsync(CancellationToken ct = default);
}
