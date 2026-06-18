using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence;

internal sealed class PlatformQueryService : IPlatformQueryService
{
    private readonly IdentityDbContext _context;

    public PlatformQueryService(IdentityDbContext context) => _context = context;

    public async Task<IReadOnlyList<TenantSummary>> GetAllTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var tenants = await _context.Tenants
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var tenantIds = tenants.Select(t => t.Id).ToList();

        var seatCounts = await _context.Users
            .Where(u => tenantIds.Contains(u.TenantId) && !u.IsDeleted)
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var lastActivity = await _context.RefreshTokens
            .Where(r => tenantIds.Contains(r.TenantId) && !r.IsDeleted)
            .GroupBy(r => r.TenantId)
            .Select(g => new { TenantId = g.Key, LastAt = g.Max(r => r.CreatedAt) })
            .ToListAsync(cancellationToken);

        var seatMap    = seatCounts.ToDictionary(x => x.TenantId, x => x.Count);
        var activityMap = lastActivity.ToDictionary(x => x.TenantId, x => x.LastAt);

        return tenants.Select(t => new TenantSummary(
            Id:             t.Id,
            Name:           t.Name,
            Slug:           t.Slug,
            Plan:           t.Plan,
            IsActive:       t.IsActive,
            SeatCount:      seatMap.GetValueOrDefault(t.Id, 0),
            CreatedAt:      t.CreatedAt,
            LastActivityAt: activityMap.TryGetValue(t.Id, out var last) ? last : null
        )).ToList().AsReadOnly();
    }

    public async Task<TenantDetail?> GetTenantDetailAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .Where(t => t.Id == tenantId && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null) return null;

        var users = await _context.Users
            .Where(u => u.TenantId == tenantId && !u.IsDeleted)
            .Include(u => u.Roles)
            .ToListAsync(cancellationToken);

        var allRoles = await _context.Roles
            .Where(r => r.TenantId == tenantId && !r.IsDeleted)
            .ToListAsync(cancellationToken);
        var roleIndex = allRoles.ToDictionary(r => r.Id, r => r.Name);

        var lastActivity = await _context.RefreshTokens
            .Where(r => r.TenantId == tenantId && !r.IsDeleted)
            .MaxAsync(r => (DateTime?)r.CreatedAt, cancellationToken);

        var userSummaries = users.Select(u =>
        {
            var roleNames = u.Roles
                .Where(ur => !ur.IsDeleted && roleIndex.ContainsKey(ur.RoleId))
                .Select(ur => roleIndex[ur.RoleId])
                .ToList().AsReadOnly();

            return new TenantUserSummary(u.Id, u.Email, u.DisplayName, u.IsActive, roleNames);
        }).ToList().AsReadOnly();

        return new TenantDetail(
            Id:             tenant.Id,
            Name:           tenant.Name,
            Slug:           tenant.Slug,
            Plan:           tenant.Plan,
            IsActive:       tenant.IsActive,
            SeatCount:      users.Count,
            CreatedAt:      tenant.CreatedAt,
            LastActivityAt: lastActivity,
            Users:          userSummaries);
    }

    public async Task<PlatformStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var totalTenants      = await _context.Tenants.CountAsync(t => !t.IsDeleted, cancellationToken);
        var activeTenants     = await _context.Tenants.CountAsync(t => !t.IsDeleted && t.IsActive, cancellationToken);
        var totalUsers        = await _context.Users.CountAsync(u => !u.IsDeleted, cancellationToken);
        var newTenantsThisWeek = await _context.Tenants.CountAsync(
            t => !t.IsDeleted && t.CreatedAt >= weekAgo, cancellationToken);

        return new PlatformStats(totalTenants, activeTenants, totalUsers, newTenantsThisWeek);
    }
}
