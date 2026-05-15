namespace STRIDE.BFF.Auth;

/// <summary>
/// Redis connection configuration for the BFF session store.
/// Bound from the "Redis" configuration section.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// StackExchange.Redis connection string.
    /// Example (local): <c>localhost:6379</c>
    /// Example (Redis Cloud TLS): <c>host:port,password=xxx,ssl=True,abortConnect=False</c>
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Optional key prefix to namespace this environment's data within the Redis instance.</summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
