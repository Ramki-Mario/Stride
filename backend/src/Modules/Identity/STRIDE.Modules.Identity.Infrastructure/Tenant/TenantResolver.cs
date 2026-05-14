using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.TenantResolution;

internal sealed class TenantResolver : ITenantResolver
{
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
        const string sql = """
            SELECT TenantId
            FROM   identity.TenantDomainMappings
            WHERE  CorporateDomain = @Domain
              AND  IsDeleted = 0
            """;

        var command = new CommandDefinition(sql, new { Domain = domain }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    private static async Task<Guid?> ResolveByUserTenantMappingAsync(
        SqlConnection connection,
        string normalizedEmail,
        CancellationToken ct)
    {
        const string sql = """
            SELECT utm.TenantId
            FROM   identity.UserTenantMappings utm
            INNER JOIN identity.Users u ON u.Id = utm.UserId
            WHERE  u.NormalizedEmail = @NormalizedEmail
              AND  u.IsDeleted = 0
              AND  utm.IsDeleted = 0
            """;

        var command = new CommandDefinition(sql, new { NormalizedEmail = normalizedEmail.ToUpperInvariant() }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Guid?>(command);
    }

    private static string ExtractDomain(string email)
    {
        var atIndex = email.LastIndexOf('@');
        return atIndex >= 0 ? email[(atIndex + 1)..] : string.Empty;
    }
}
