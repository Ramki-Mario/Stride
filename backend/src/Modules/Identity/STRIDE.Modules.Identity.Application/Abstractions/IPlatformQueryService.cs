namespace STRIDE.Modules.Identity.Application.Abstractions;

public sealed record TenantSummary(
    Guid     Id,
    string   Name,
    string   Slug,
    string   Plan,
    bool     IsActive,
    int      SeatCount,
    DateTime CreatedAt,
    DateTime? LastActivityAt);

public sealed record TenantDetail(
    Guid     Id,
    string   Name,
    string   Slug,
    string   Plan,
    bool     IsActive,
    int      SeatCount,
    DateTime CreatedAt,
    DateTime? LastActivityAt,
    IReadOnlyList<TenantUserSummary> Users);

public sealed record TenantUserSummary(
    Guid   Id,
    string Email,
    string DisplayName,
    bool   IsActive,
    IReadOnlyList<string> Roles);

public sealed record PlatformStats(
    int TotalTenants,
    int ActiveTenants,
    int TotalUsers,
    int NewTenantsThisWeek);

public interface IPlatformQueryService
{
    Task<IReadOnlyList<TenantSummary>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    Task<TenantDetail?> GetTenantDetailAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PlatformStats> GetStatsAsync(CancellationToken cancellationToken = default);
}
