using System.Collections.Concurrent;

namespace STRIDE.BFF.Auth;

/// <summary>
/// Singleton that vends one <see cref="SemaphoreSlim"/> per authenticated user.
/// Ensures only one concurrent token-refresh call fires for a given user, preventing
/// refresh-token rotation conflicts when multiple parallel requests all see an
/// expiring access token at the same time.
/// </summary>
public sealed class TokenRenewalLockProvider
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public SemaphoreSlim GetOrCreate(Guid userId)
        => _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
}
