using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Infrastructure.Services;

/// <summary>
/// Cross-module read bridge to <c>identity.Users</c>.
/// Resolves user IDs from email local-parts (the segment before '@') using Dapper so
/// no project reference to Identity.Application is needed.
/// </summary>
internal sealed class UserLookupService : IUserLookupService
{
    private readonly IDbConnectionFactory _db;

    public UserLookupService(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyDictionary<string, Guid>> LookupByEmailLocalPartsAsync(
        Guid                tenantId,
        IEnumerable<string> localParts,
        CancellationToken   ct = default)
    {
        var parts = localParts.Select(p => p.ToLowerInvariant()).Distinct().ToList();
        if (parts.Count == 0)
            return new Dictionary<string, Guid>();

        // LOWER(LEFT(Email, CHARINDEX('@', Email) - 1)) extracts the local-part from the
        // stored email address so we can do a case-insensitive exact match.
        const string sql = """
            SELECT LOWER(LEFT(Email, CHARINDEX('@', Email) - 1)) AS LocalPart,
                   Id AS UserId
            FROM   [identity].[Users]
            WHERE  TenantId  = @TenantId
              AND  IsActive   = 1
              AND  IsDeleted  = 0
              AND  LOWER(LEFT(Email, CHARINDEX('@', Email) - 1)) IN @LocalParts
            """;

        await using var conn = await _db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(string LocalPart, Guid UserId)>(
            new CommandDefinition(sql, new { TenantId = tenantId, LocalParts = parts },
                cancellationToken: ct));

        return rows.ToDictionary(r => r.LocalPart, r => r.UserId);
    }
}
