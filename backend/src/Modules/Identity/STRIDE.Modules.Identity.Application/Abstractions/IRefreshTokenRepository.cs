using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    /// <summary>Looks up an active refresh token by its raw token string (tenant-scoped).</summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Stages a new refresh token for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

    /// <summary>Revokes all non-revoked tokens for a given user within the current tenant.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTime utcNow, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
