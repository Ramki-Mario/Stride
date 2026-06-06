using System.Data.Common;
using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.TenantResolution;

internal sealed class TenantResolver : ITenantResolver
{
    private static readonly string SqlResolveByCorporateDomain =
        SqlLoader.Load(typeof(TenantResolver).Assembly,
            "STRIDE.Modules.Identity.Infrastructure.Tenant.Queries.ResolveTenantByCorporateDomain.sql");

    private static readonly string SqlResolveByUserMapping =
        SqlLoader.Load(typeof(TenantResolver).Assembly,
            "STRIDE.Modules.Identity.Infrastructure.Tenant.Queries.ResolveTenantByUserMapping.sql");

    private readonly IDbConnectionFactory _db;

    public TenantResolver(IDbConnectionFactory db) => _db = db;

    public async Task<Guid?> ResolveFromEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var domain          = ExtractDomain(normalizedEmail);

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);

        if (!GenericEmailDomains.IsGeneric(domain))
        {
            var tenantId = await ResolveByCorporateDomainAsync(conn, domain, cancellationToken);
            if (tenantId.HasValue)
                return tenantId;
        }

        return await ResolveByUserTenantMappingAsync(conn, normalizedEmail, cancellationToken);
    }

    private static Task<Guid?> ResolveByCorporateDomainAsync(
        DbConnection conn, string domain, CancellationToken cancellationToken)
        => conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(SqlResolveByCorporateDomain, new { Domain = domain }, cancellationToken: cancellationToken));

    private static Task<Guid?> ResolveByUserTenantMappingAsync(
        DbConnection conn, string normalizedEmail, CancellationToken cancellationToken)
        => conn.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(SqlResolveByUserMapping,
                new { NormalizedEmail = normalizedEmail.ToUpperInvariant() },
                cancellationToken: cancellationToken));

    private static string ExtractDomain(string email)
    {
        var atIndex = email.LastIndexOf('@');
        return atIndex >= 0 ? email[(atIndex + 1)..] : string.Empty;
    }
}
