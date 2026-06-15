using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence;

internal sealed class KitReservationRepository : IKitReservationRepository
{
    private readonly KitOpsDbContext _db;

    public KitReservationRepository(KitOpsDbContext db) => _db = db;

    public Task<KitReservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.KitReservations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<KitReservation>> GetPendingByKitItemIdAsync(
        Guid kitItemId, CancellationToken ct = default)
        => await _db.KitReservations
            .Where(r => r.KitItemId == kitItemId && r.Status == KitReservationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<KitReservation?> GetOldestPendingByKitItemIdAsync(
        Guid kitItemId, CancellationToken ct = default)
        => _db.KitReservations
            .Where(r => r.KitItemId == kitItemId && r.Status == KitReservationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(KitReservation reservation, CancellationToken ct = default)
        => await _db.KitReservations.AddAsync(reservation, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
