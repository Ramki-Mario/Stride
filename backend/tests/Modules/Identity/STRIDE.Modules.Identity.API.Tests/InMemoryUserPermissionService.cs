using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.API.Tests;

/// <summary>
/// In-memory <see cref="IUserPermissionService"/> for authorization handler tests — a simple
/// dictionary-backed permission store keyed by (tenantId, userId).
/// </summary>
internal sealed class InMemoryUserPermissionService : IUserPermissionService
{
    private readonly Dictionary<(Guid Tenant, Guid User), IReadOnlySet<string>> _store = new();

    public int InvalidateCallCount { get; private set; }

    public void Seed(Guid tenantId, Guid userId, params string[] keys)
        => _store[(tenantId, userId)] = keys.ToHashSet(StringComparer.Ordinal);

    public Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(
            _store.TryGetValue((tenantId, userId), out var set)
                ? set
                : (IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal));

    public void InvalidateUser(Guid tenantId, Guid userId)
    {
        InvalidateCallCount++;
        _store.Remove((tenantId, userId));
    }
}
