using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository
    : TenantAwareRepository<RefreshToken, IdentityDbContext>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await Query.FirstOrDefaultAsync(r => r.Token == token, ct);

    public async Task<RefreshToken?> GetByTokenCrossTenantAsync(string token, CancellationToken ct = default)
        => await Context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token && !r.IsDeleted, ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
        => await Context.RefreshTokens.AddAsync(refreshToken, ct);

    public async Task RevokeAllForUserAsync(Guid userId, DateTime utcNow, CancellationToken ct = default)
    {
        var active = await Query
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > utcNow)
            .ToListAsync(ct);

        foreach (var t in active)
            t.Revoke(utcNow);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
