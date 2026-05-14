using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Identity.Domain.Events;
using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class User : AuditableEntity
{
    private readonly List<UserRole> _roles = new();

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public Password PasswordHash { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public IReadOnlyList<UserRole> Roles => _roles.AsReadOnly();

    private User() { }

    public static User Create(
        Guid tenantId,
        string email,
        string displayName,
        Password passwordHash,
        Guid createdBy)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.Trim(),
            NormalizedEmail = email.Trim().ToUpperInvariant(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, tenantId, user.Email));
        return user;
    }

    public void AssignRole(Role role, Guid assignedBy)
    {
        if (_roles.Any(r => r.RoleId == role.Id && !r.IsDeleted))
            return;

        var userRole = UserRole.Create(TenantId, Id, role.Id, assignedBy);
        _roles.Add(userRole);
        RaiseDomainEvent(new UserRoleAssignedEvent(Id, TenantId, role.Id));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(Password newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(Guid deletedBy)
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
