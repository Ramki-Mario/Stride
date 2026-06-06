using System.Data.Common;

namespace STRIDE.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// MySQL / MariaDB implementation of <see cref="IDbConnectionFactory"/>.
///
/// USAGE:
///   1. Install NuGet: Pomelo.EntityFrameworkCore.MySql  (recommended)
///              or:  MySql.Data
///   2. Uncomment the body below.
///   3. In InfrastructureServiceExtensions replace SqlServerConnectionFactory
///      registration with MySqlConnectionFactory — nothing else needs changing.
///
/// Raw SQL notes when targeting MySQL:
///   - Replace  TOP 1          → LIMIT 1
///   - Replace  GETUTCDATE()   → UTC_TIMESTAMP()
///   - Replace  NEWID()        → UUID()
///   - Replace  CAST(1 AS BIT) → 1
///   - Square-bracket escaping [col] → backtick `col`
/// </summary>
public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    public Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "MySqlConnectionFactory is a placeholder. " +
            "Install Pomelo.EntityFrameworkCore.MySql and uncomment the implementation.");
}
