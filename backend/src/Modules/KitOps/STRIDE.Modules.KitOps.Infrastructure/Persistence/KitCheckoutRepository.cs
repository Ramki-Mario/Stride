using System.Data;
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

    public async Task<bool> TryCheckoutAsync(KitItem item, KitCheckout checkout, CancellationToken ct = default)
    {
        // Concurrency safety: a SERIALIZABLE transaction plus an explicit update-lock on the
        // KitItem row serializes concurrent checkouts of the *same* item. The second transaction
        // blocks on the row lock until the first commits, then re-evaluates availability — so the
        // last available unit can never be double-issued. Checkouts of different items don't
        // contend (the lock is row-scoped).
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM [kitops].[KitItems] WITH (UPDLOCK, HOLDLOCK) WHERE Id = {item.Id}", ct);

        var outstanding = await _db.KitCheckouts
            .CountAsync(c => c.KitItemId == item.Id && c.Status != KitCheckoutStatus.Returned, ct);

        if (outstanding >= item.TotalQuantity)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        await _db.KitCheckouts.AddAsync(checkout, ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public Task<int> GetOutstandingCountByKitItemIdAsync(Guid kitItemId, CancellationToken ct = default)
        => _db.KitCheckouts.CountAsync(
            c => c.KitItemId == kitItemId && c.Status != KitCheckoutStatus.Returned,
            ct);

    public Task<int> GetOverdueCountByKitItemIdAsync(Guid kitItemId, DateTime asOf, CancellationToken ct = default)
        => _db.KitCheckouts.CountAsync(
            c => c.KitItemId == kitItemId
              && c.Status != KitCheckoutStatus.Returned
              && c.ExpectedReturnAt < asOf,
            ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
