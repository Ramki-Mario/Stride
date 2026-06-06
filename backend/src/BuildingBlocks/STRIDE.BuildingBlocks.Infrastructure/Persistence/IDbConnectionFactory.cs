using System.Data.Common;

namespace STRIDE.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Abstracts database connection creation and opening so every Dapper
/// read/write service is decoupled from a specific ADO.NET provider.
///
/// Switching databases requires only two steps:
///   1. Register a different implementation (e.g. MySqlConnectionFactory)
///      in <c>InfrastructureServiceExtensions</c>.
///   2. Keep the old implementation in the workspace — nothing else changes.
///
/// The factory opens the connection before returning it so callers never
/// forget to call OpenAsync, and the cancellation token is always forwarded
/// to the TCP handshake.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a new database connection.
    /// Caller is responsible for disposal (use <c>await using</c>).
    /// </summary>
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
