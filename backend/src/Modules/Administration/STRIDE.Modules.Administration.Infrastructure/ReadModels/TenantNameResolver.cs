using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Infrastructure.ReadModels;

/// <summary>
/// Cross-schema Dapper read that resolves a tenant's display name from
/// <c>[identity].Tenants</c> without coupling the Administration module to the
/// Identity domain model.
/// </summary>
internal sealed class TenantNameResolver : ITenantNameResolver
{
    private readonly IDbConnectionFactory _db;

    public TenantNameResolver(IDbConnectionFactory db) => _db = db;

    public async Task<string> ResolveAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var name = await conn.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                "SELECT TOP 1 Name FROM [identity].Tenants WHERE Id = @TenantId AND IsDeleted = 0",
                new { TenantId = tenantId },
                cancellationToken: ct));

        return name ?? string.Empty;
    }
}
