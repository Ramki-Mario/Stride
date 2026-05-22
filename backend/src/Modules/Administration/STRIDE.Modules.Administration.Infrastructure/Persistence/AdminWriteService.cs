using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// Dapper-based write service for Administration commands that mutate users
/// in the <c>identity.*</c> schema without coupling to Identity.Domain entities.
/// </summary>
internal sealed class AdminWriteService : IAdminWriteService
{
    private readonly string _connectionString;

    public AdminWriteService(IConfiguration configuration)
        => _connectionString = configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    // ── Invite ────────────────────────────────────────────────────────────────

    public async Task<Guid> InviteUserAsync(
        Guid tenantId,
        string email,
        string displayName,
        string roleName,
        Guid invitedBy,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var userId = Guid.NewGuid();
        var now    = DateTime.UtcNow;

        // 1. Resolve role ID by name
        var roleId = await conn.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                "SELECT TOP 1 Id FROM identity.Roles WHERE TenantId = @TenantId AND NormalizedName = @Name AND IsDeleted = 0",
                new { TenantId = tenantId, Name = roleName.ToUpperInvariant() },
                transaction: tx, cancellationToken: ct));

        if (roleId is null)
            throw new InvalidOperationException($"Role '{roleName}' not found for tenant {tenantId}.");

        // 2. Check email uniqueness within tenant
        var exists = await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "SELECT CAST(1 AS BIT) FROM identity.Users WHERE TenantId = @TenantId AND NormalizedEmail = @Email AND IsDeleted = 0",
                new { TenantId = tenantId, Email = email.ToUpperInvariant() },
                transaction: tx, cancellationToken: ct));

        if (exists)
            throw new InvalidOperationException($"A user with email '{email}' already exists in this tenant.");

        // 3. Insert User (IsPending=1, IsActive=0, placeholder password hash)
        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO identity.Users
                    (Id, TenantId, Email, NormalizedEmail, DisplayName, PasswordHash,
                     IsActive, IsPending, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
                VALUES
                    (@Id, @TenantId, @Email, @NormalizedEmail, @DisplayName, @PasswordHash,
                     0, 1, @Now, @Now, @CreatedBy, 0)
                """,
                new
                {
                    Id              = userId,
                    TenantId        = tenantId,
                    Email           = email.Trim(),
                    NormalizedEmail = email.Trim().ToUpperInvariant(),
                    DisplayName     = displayName.Trim(),
                    PasswordHash    = "PENDING_INVITE", // placeholder; cannot log in until activated
                    Now             = now,
                    CreatedBy       = invitedBy
                },
                transaction: tx, cancellationToken: ct));

        // 4. Assign role
        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO identity.UserRoles
                    (Id, TenantId, UserId, RoleId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
                VALUES
                    (@Id, @TenantId, @UserId, @RoleId, @Now, @Now, @CreatedBy, 0)
                """,
                new
                {
                    Id        = Guid.NewGuid(),
                    TenantId  = tenantId,
                    UserId    = userId,
                    RoleId    = roleId.Value,
                    Now       = now,
                    CreatedBy = invitedBy
                },
                transaction: tx, cancellationToken: ct));

        // 5. Create UserTenantMapping (required for TenantResolver fallback on login)
        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO identity.UserTenantMappings
                    (Id, TenantId, UserId, Role, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
                VALUES
                    (@Id, @TenantId, @UserId, @Role, @Now, @Now, @CreatedBy, 0)
                """,
                new
                {
                    Id        = Guid.NewGuid(),
                    TenantId  = tenantId,
                    UserId    = userId,
                    Role      = roleName,
                    Now       = now,
                    CreatedBy = invitedBy
                },
                transaction: tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return userId;
    }

    // ── Update Role ───────────────────────────────────────────────────────────

    public async Task UpdateUserRoleAsync(
        Guid tenantId, Guid userId, string newRoleName, Guid updatedBy, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var roleId = await conn.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                "SELECT TOP 1 Id FROM identity.Roles WHERE TenantId = @TenantId AND NormalizedName = @Name AND IsDeleted = 0",
                new { TenantId = tenantId, Name = newRoleName.ToUpperInvariant() },
                transaction: tx, cancellationToken: ct));

        if (roleId is null)
            throw new InvalidOperationException($"Role '{newRoleName}' not found.");

        // Soft-delete existing roles for this user in this tenant
        await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE identity.UserRoles SET IsDeleted = 1, UpdatedAt = @Now WHERE TenantId = @TenantId AND UserId = @UserId AND IsDeleted = 0",
                new { TenantId = tenantId, UserId = userId, Now = DateTime.UtcNow },
                transaction: tx, cancellationToken: ct));

        // Insert new role assignment
        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO identity.UserRoles
                    (Id, TenantId, UserId, RoleId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted)
                VALUES
                    (@Id, @TenantId, @UserId, @RoleId, @Now, @Now, @CreatedBy, 0)
                """,
                new
                {
                    Id        = Guid.NewGuid(),
                    TenantId  = tenantId,
                    UserId    = userId,
                    RoleId    = roleId.Value,
                    Now       = DateTime.UtcNow,
                    CreatedBy = updatedBy
                },
                transaction: tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    public async Task DeactivateUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE identity.Users SET IsActive = 0, UpdatedAt = @Now WHERE TenantId = @TenantId AND Id = @UserId AND IsDeleted = 0",
                new { TenantId = tenantId, UserId = userId, Now = DateTime.UtcNow },
                cancellationToken: ct));
    }

    // ── Reactivate ────────────────────────────────────────────────────────────

    public async Task ReactivateUserAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.ExecuteAsync(
            new CommandDefinition(
                "UPDATE identity.Users SET IsActive = 1, IsPending = 0, UpdatedAt = @Now WHERE TenantId = @TenantId AND Id = @UserId AND IsDeleted = 0",
                new { TenantId = tenantId, UserId = userId, Now = DateTime.UtcNow },
                cancellationToken: ct));
    }
}
