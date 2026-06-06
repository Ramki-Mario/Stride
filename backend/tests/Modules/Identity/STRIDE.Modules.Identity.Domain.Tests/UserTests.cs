using FluentAssertions;
using STRIDE.Modules.Identity.Domain.Entities;
using STRIDE.Modules.Identity.Domain.Events;
using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Domain.Tests;

public sealed class UserTests
{
    private static readonly Password TestHash = Password.FromHash("hashed_password_value");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsActiveNonPendingUser()
    {
        // Arrange
        var tenantId  = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var user = User.Create(tenantId, "alice@company.com", "Alice Smith", TestHash, createdBy);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.TenantId.Should().Be(tenantId);
        user.Email.Should().Be("alice@company.com");
        user.DisplayName.Should().Be("Alice Smith");
        user.IsActive.Should().BeTrue();
        user.IsPending.Should().BeFalse();
        user.IsDeleted.Should().BeFalse();
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Create_NormalisesEmailToUppercase()
    {
        // Act
        var user = User.Create(Guid.NewGuid(), "Alice@Company.com", "Alice", TestHash, Guid.NewGuid());

        // Assert
        user.NormalizedEmail.Should().Be("ALICE@COMPANY.COM");
    }

    [Fact]
    public void Create_TrimsEmailAndDisplayName()
    {
        // Act
        var user = User.Create(Guid.NewGuid(), "  alice@a.com  ", "  Alice  ", TestHash, Guid.NewGuid());

        // Assert
        user.Email.Should().Be("alice@a.com");
        user.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public void Create_RaisesUserCreatedEvent()
    {
        // Act
        var user = User.Create(Guid.NewGuid(), "alice@a.com", "Alice", TestHash, Guid.NewGuid());

        // Assert
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedEvent>();
    }

    // ── Invite ────────────────────────────────────────────────────────────────

    [Fact]
    public void Invite_CreatesInactiveAndPendingUser()
    {
        // Act
        var user = User.Invite(
            Guid.NewGuid(), "bob@company.com", "Bob Jones",
            Password.FromHash("PENDING"), Guid.NewGuid());

        // Assert
        user.IsActive.Should().BeFalse();
        user.IsPending.Should().BeTrue();
    }

    // ── Deactivate / Reactivate ───────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var user = User.Create(Guid.NewGuid(), "alice@a.com", "Alice", TestHash, Guid.NewGuid());
        user.ClearDomainEvents();

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_SetsIsActiveTrueAndIsPendingFalse()
    {
        // Arrange
        var user = User.Invite(
            Guid.NewGuid(), "bob@a.com", "Bob",
            Password.FromHash("PENDING"), Guid.NewGuid());
        user.ClearDomainEvents();

        // Act
        user.Reactivate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.IsPending.Should().BeFalse();
    }

    // ── AssignRole ────────────────────────────────────────────────────────────

    [Fact]
    public void AssignRole_AddsRoleToUserRolesCollection()
    {
        // Arrange
        var user      = User.Create(Guid.NewGuid(), "alice@a.com", "Alice", TestHash, Guid.NewGuid());
        var role      = BuildRole("Admin");
        user.ClearDomainEvents();

        // Act
        user.AssignRole(role, Guid.NewGuid());

        // Assert
        user.Roles.Should().HaveCount(1);
        user.Roles[0].RoleId.Should().Be(role.Id);
    }

    [Fact]
    public void AssignRole_SameRoleTwice_DoesNotDuplicate()
    {
        // Arrange
        var user = User.Create(Guid.NewGuid(), "alice@a.com", "Alice", TestHash, Guid.NewGuid());
        var role = BuildRole("Admin");
        user.ClearDomainEvents();

        // Act
        user.AssignRole(role, Guid.NewGuid());
        user.AssignRole(role, Guid.NewGuid());

        // Assert
        user.Roles.Should().HaveCount(1, "duplicate role assignment must be silently ignored");
    }

    [Fact]
    public void AssignRole_RaisesUserRoleAssignedEvent()
    {
        // Arrange
        var user = User.Create(Guid.NewGuid(), "alice@a.com", "Alice", TestHash, Guid.NewGuid());
        user.ClearDomainEvents();

        // Act
        user.AssignRole(BuildRole("Member"), Guid.NewGuid());

        // Assert
        user.DomainEvents.Should().Contain(e => e is UserRoleAssignedEvent);
    }

    // ── Password / DisplayName update ─────────────────────────────────────────

    [Fact]
    public void UpdatePassword_ReplacesPasswordHash()
    {
        // Arrange
        var user    = User.Create(Guid.NewGuid(), "alice@a.com", "Alice", TestHash, Guid.NewGuid());
        var newHash = Password.FromHash("new_hash_value");

        // Act
        user.UpdatePassword(newHash);

        // Assert
        user.PasswordHash.Hash.Should().Be("new_hash_value");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_SetsIsDeletedTrue()
    {
        // Arrange
        var user = User.Create(Guid.NewGuid(), "alice@a.com", "Alice", TestHash, Guid.NewGuid());
        user.ClearDomainEvents();

        // Act
        user.Delete(Guid.NewGuid());

        // Assert
        user.IsDeleted.Should().BeTrue();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Role BuildRole(string name)
        => Role.Create(Guid.NewGuid(), name, $"{name} role", Guid.NewGuid());
}
