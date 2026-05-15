namespace STRIDE.BFF.Auth;

/// <summary>
/// Composite cookie value: {tenantId}.{opaqueId}.
/// Encodes both the tenant scope and a random opaque session identifier so that
/// <see cref="RedisTicketStore"/> can locate the right Redis key
/// (<c>tenant:{tenantId:N}:session:{opaqueId}</c>) given only the cookie's value.
/// </summary>
internal readonly record struct SessionCookieKey(Guid TenantId, string OpaqueId)
{
    public string Render() => $"{TenantId:N}.{OpaqueId}";

    public string ToRedisKey(string keyPrefix) =>
        string.IsNullOrEmpty(keyPrefix)
            ? $"tenant:{TenantId:N}:session:{OpaqueId}"
            : $"{keyPrefix}:tenant:{TenantId:N}:session:{OpaqueId}";

    public static bool TryParse(string raw, out SessionCookieKey key)
    {
        key = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var dot = raw.IndexOf('.');
        if (dot <= 0 || dot >= raw.Length - 1) return false;

        var tenantPart = raw[..dot];
        var opaquePart = raw[(dot + 1)..];

        if (!Guid.TryParseExact(tenantPart, "N", out var tenantId)) return false;
        if (string.IsNullOrWhiteSpace(opaquePart)) return false;

        key = new SessionCookieKey(tenantId, opaquePart);
        return true;
    }
}
