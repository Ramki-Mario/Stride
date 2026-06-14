using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Infrastructure.Persistence;

namespace STRIDE.Modules.Identity.Infrastructure.Authorization;

/// <summary>
/// Resolves a user's effective permission keys from the database and caches them
/// in-process per <c>(tenantId, userId)</c> for a short window. Because the Host is
/// a single-process modular monolith, an in-memory cache is sufficient — there is no
/// second node whose cache could go stale. Invalidation is explicit on role change.
/// </summary>
internal sealed class UserPermissionService : IUserPermissionService
{
    /// <summary>Short TTL bounds staleness if an invalidation is ever missed.</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IdentityDbContext _db;
    private readonly IMemoryCache _cache;

    public UserPermissionService(IdentityDbContext db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty)
            return EmptySet;

        var key = CacheKey(tenantId, userId);

        if (_cache.TryGetValue(key, out IReadOnlySet<string>? cached) && cached is not null)
            return cached;

        var permissions = await LoadFromDatabaseAsync(tenantId, userId, cancellationToken);

        _cache.Set(key, permissions, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        });

        return permissions;
    }

    public void InvalidateUser(Guid tenantId, Guid userId)
        => _cache.Remove(CacheKey(tenantId, userId));

    private async Task<IReadOnlySet<string>> LoadFromDatabaseAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // UserRoles → RolePermissions → Permissions, tenant-scoped, ignoring soft-deleted links.
        var keys = await (
            from ur in _db.UserRoles.AsNoTracking()
            where ur.UserId == userId && ur.TenantId == tenantId && !ur.IsDeleted
            join rp in _db.RolePermissions.AsNoTracking()
                on ur.RoleId equals rp.RoleId
            where !rp.IsDeleted && rp.TenantId == tenantId
            join p in _db.Permissions.AsNoTracking()
                on rp.PermissionId equals p.Id
            select p.Key)
            .Distinct()
            .ToListAsync(cancellationToken);

        return keys.ToHashSet(StringComparer.Ordinal);
    }

    private static string CacheKey(Guid tenantId, Guid userId)
        => $"permissions:{tenantId:N}:{userId:N}";

    private static readonly IReadOnlySet<string> EmptySet =
        new HashSet<string>(StringComparer.Ordinal);
}
