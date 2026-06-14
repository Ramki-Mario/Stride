namespace STRIDE.Modules.Administration.Application.Abstractions;

public interface IAdminWriteService
{
    /// <summary>Creates an invited (pending) user in the identity schema via Dapper.</summary>
    Task<Guid> InviteUserAsync(
        Guid tenantId,
        string email,
        string displayName,
        string roleName,
        Guid invitedBy,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a SHA-256 invite token hash for the given user.</summary>
    Task CreateInviteTokenAsync(
        Guid     tenantId,
        Guid     userId,
        string   tokenHash,
        DateTime expiresAt,
        Guid     createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>Changes the primary role for a user within a tenant.</summary>
    Task UpdateUserRoleAsync(Guid tenantId, Guid userId, string newRoleName, Guid updatedBy, CancellationToken cancellationToken = default);

    /// <summary>Sets IsActive = false on a user.</summary>
    Task DeactivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Sets IsActive = true, IsPending = false on a user.</summary>
    Task ReactivateUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}
