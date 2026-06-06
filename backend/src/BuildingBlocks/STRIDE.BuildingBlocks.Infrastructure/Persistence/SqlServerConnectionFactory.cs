using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace STRIDE.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// SQL Server / Azure SQL implementation of <see cref="IDbConnectionFactory"/>.
///
/// To switch to MySQL:
///   - Add Pomelo.EntityFrameworkCore.MySql (or MySql.Data) NuGet package.
///   - Register <c>MySqlConnectionFactory</c> in DI instead of this class.
///   - This file stays in the workspace untouched — swap back any time.
/// </summary>
public sealed class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlServerConnectionFactory(string connectionString)
        => _connectionString = connectionString;

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        return conn;
    }
}
