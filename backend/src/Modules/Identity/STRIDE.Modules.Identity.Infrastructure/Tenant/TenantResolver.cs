using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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

    private readonly string _connectionString;

    public TenantResolver(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<Guid?> ResolveFromEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var domain = ExtractDomain(normalizedEmail);

        await using var connection = new SqlConnection(_connectionString);

        if (!GenericEmailDomains.IsGeneric(domain))
        {
            var tenantId = await ResolveByCorporateDomainAsync(connection, domain, ct);
            if (tenantId.HasValue)
                return tenantId;
        }

        return await ResolveByUserTenantMappingAsync(connection, normalizedEmail, ct);
    }

    private static async Task<Guid?> ResolveByCorporateDomainAsync(
        SqlConnection connection,
        string domain,
        CancellationToken ct)
    {
        var command = new CommandDefinition(SqlResolveByCorporateDomain, new { Domain = domain }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    private static async Task<Guid?> ResolveByUserTenantMappingAsync(
        SqlConnection connection,
        string normalizedEmail,
        CancellationToken ct)
    {
        var command = new CommandDefinition(SqlResolveByUserMapping, new { NormalizedEmail = normalizedEmail.ToUpperInvariant() }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    private static string ExtractDomain(string email)
    {
        var atIndex = email.LastIndexOf('@');
        return atIndex >= 0 ? email[(atIndex + 1)..] : string.Empty;
    }
}
