using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace STRIDE.BFF.Auth;

/// <summary>
/// Redis-backed implementation of <see cref="ITicketStore"/>.
/// Stores serialized <see cref="AuthenticationTicket"/> instances under
/// <c>tenant:{tenantId:N}:session:{opaqueId}</c>, so sessions are tenant-namespaced
/// in Redis and there is no cross-tenant key collision risk.
/// </summary>
internal sealed class RedisTicketStore : ITicketStore
{
    private const string TenantClaimType = "tid";
    private const int OpaqueIdByteLength = 32;

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;

    public RedisTicketStore(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var tenantId = ExtractTenantId(ticket)
            ?? throw new InvalidOperationException(
                "Cannot create session — the principal has no 'tid' (tenant) claim.");

        var key = new SessionCookieKey(tenantId, NewOpaqueId());
        await PersistAsync(key, ticket);
        return key.Render();
    }

    public async Task RenewAsync(string cookieValue, AuthenticationTicket ticket)
    {
        if (!SessionCookieKey.TryParse(cookieValue, out var key))
            return;

        await PersistAsync(key, ticket);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string cookieValue)
    {
        if (!SessionCookieKey.TryParse(cookieValue, out var key))
            return null;

        var db = _redis.GetDatabase();
        var bytes = (byte[]?)await db.StringGetAsync(key.ToRedisKey(_options.KeyPrefix));
        if (bytes is null || bytes.Length == 0) return null;

        return TicketSerializer.Default.Deserialize(bytes);
    }

    public async Task RemoveAsync(string cookieValue)
    {
        if (!SessionCookieKey.TryParse(cookieValue, out var key))
            return;

        await _redis.GetDatabase().KeyDeleteAsync(key.ToRedisKey(_options.KeyPrefix));
    }

    private async Task PersistAsync(SessionCookieKey key, AuthenticationTicket ticket)
    {
        var bytes = TicketSerializer.Default.Serialize(ticket);
        var ttl = TicketTtl(ticket);

        await _redis.GetDatabase().StringSetAsync(
            key.ToRedisKey(_options.KeyPrefix),
            bytes,
            ttl);
    }

    private static Guid? ExtractTenantId(AuthenticationTicket ticket)
    {
        var raw = ticket.Principal.FindFirst(TenantClaimType)?.Value;
        return Guid.TryParseExact(raw, "N", out var id) ? id : null;
    }

    private static TimeSpan TicketTtl(AuthenticationTicket ticket)
    {
        var expires = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(8);
        var ttl = expires - DateTimeOffset.UtcNow;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromMinutes(1);
    }

    private static string NewOpaqueId()
    {
        var bytes = RandomNumberGenerator.GetBytes(OpaqueIdByteLength);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
