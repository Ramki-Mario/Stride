using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace STRIDE.BFF.Auth;

/// <summary>
/// Development-only in-process ticket store backed by <see cref="IMemoryCache"/>.
/// Avoids the Redis Cloud SSL dependency so the BFF can be run locally without
/// a working Redis connection.  Sessions are lost on app restart — fine for dev.
///
/// Not suitable for production: sessions are not shared across instances and
/// are not revocable from outside the process.  Use <see cref="RedisTicketStore"/>
/// in Production.
/// </summary>
internal sealed class InMemoryTicketStore : ITicketStore
{
    private readonly IMemoryCache _cache;

    public InMemoryTicketStore(IMemoryCache cache) => _cache = cache;

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString("N");
        var ttl = ticket.Properties.ExpiresUtc.HasValue
            ? ticket.Properties.ExpiresUtc.Value - DateTimeOffset.UtcNow
            : TimeSpan.FromHours(8);

        _cache.Set(key, ticket, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl > TimeSpan.Zero ? ttl : TimeSpan.FromMinutes(1)
        });

        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        StoreAsync(ticket); // overwrites with fresh TTL
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        _cache.TryGetValue<AuthenticationTicket>(key, out var ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
