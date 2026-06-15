using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence;

internal sealed class KitItemRepository : IKitItemRepository
{
    private readonly KitOpsDbContext _db;

    public KitItemRepository(KitOpsDbContext db) => _db = db;

    public Task<KitItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.KitItems.FirstOrDefaultAsync(k => k.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        => _db.KitItems.AnyAsync(
            k => k.Name == name && (excludeId == null || k.Id != excludeId),
            ct);

    public async Task AddAsync(KitItem item, CancellationToken ct = default)
        => await _db.KitItems.AddAsync(item, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
