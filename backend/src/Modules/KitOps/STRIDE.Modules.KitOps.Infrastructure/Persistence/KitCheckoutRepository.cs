using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence;

internal sealed class KitCheckoutRepository : IKitCheckoutRepository
{
    private readonly KitOpsDbContext _db;

    public KitCheckoutRepository(KitOpsDbContext db) => _db = db;

    public Task<KitCheckout?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.KitCheckouts.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<int> GetActiveCountByKitItemIdAsync(Guid kitItemId, CancellationToken ct = default)
        => _db.KitCheckouts.CountAsync(
            c => c.KitItemId == kitItemId && c.Status == KitCheckoutStatus.Active,
            ct);

    public async Task<IReadOnlyList<KitCheckout>> GetActiveCheckoutsForKitItemAsync(
        Guid kitItemId, CancellationToken ct = default)
        => await _db.KitCheckouts
            .Where(c => c.KitItemId == kitItemId && c.Status == KitCheckoutStatus.Active)
            .ToListAsync(ct);

    public async Task AddAsync(KitCheckout checkout, CancellationToken ct = default)
        => await _db.KitCheckouts.AddAsync(checkout, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
