using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class InviteTokenRepository
    : TenantAwareRepository<InviteToken, IdentityDbContext>, IInviteTokenRepository
{
    public InviteTokenRepository(IdentityDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task AddAsync(InviteToken token, CancellationToken ct = default)
        => await Context.InviteTokens.AddAsync(token, ct);

    /// <summary>
    /// Looks up by hash without tenant scope — the caller presents only the raw token
    /// and has no JWT yet (unauthenticated endpoint).
    /// </summary>
    public async Task<InviteToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => await Context.InviteTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsDeleted, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
