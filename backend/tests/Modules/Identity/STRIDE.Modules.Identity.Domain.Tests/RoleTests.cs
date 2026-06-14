using FluentAssertions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Tests;

public sealed class RoleTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();

    [Fact]
    public void Create_ProducesNonSystemRole()
    {
        var role = Role.Create(TenantId, "FieldWorker", "Field worker role", ActorId);

        role.IsSystemRole.Should().BeFalse();
        role.Name.Should().Be("FieldWorker");
        role.NormalizedName.Should().Be("FIELDWORKER");
        role.TenantId.Should().Be(TenantId);
        role.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreateSystemRole_ProducesIsSystemRoleTrue()
    {
        var role = Role.CreateSystemRole(TenantId, "TenantAdmin", "Full admin", ActorId);

        role.IsSystemRole.Should().BeTrue();
        role.Name.Should().Be("TenantAdmin");
        role.NormalizedName.Should().Be("TENANTADMIN");
    }

    [Fact]
    public void GrantPermission_AddsPermissionToRole()
    {
        var role       = Role.Create(TenantId, "TestRole", "desc", ActorId);
        var permission = Permission.Create("workflow.view", "View workflows");

        role.GrantPermission(permission, ActorId);

        role.Permissions.Should().HaveCount(1);
        role.Permissions[0].PermissionId.Should().Be(permission.Id);
        role.Permissions[0].RoleId.Should().Be(role.Id);
    }

    [Fact]
    public void GrantPermission_WhenAlreadyGranted_DoesNotDuplicate()
    {
        var role       = Role.Create(TenantId, "TestRole", "desc", ActorId);
        var permission = Permission.Create("workflow.view", "View workflows");

        role.GrantPermission(permission, ActorId);
        role.GrantPermission(permission, ActorId); // second call is no-op

        role.Permissions.Should().HaveCount(1);
    }

    [Fact]
    public void RevokePermission_SetsIsDeletedOnRolePermission()
    {
        var role       = Role.Create(TenantId, "TestRole", "desc", ActorId);
        var permission = Permission.Create("workflow.view", "View workflows");
        role.GrantPermission(permission, ActorId);

        role.RevokePermission(permission.Id);

        role.Permissions[0].IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void GrantPermission_AllDefaultPermissions_GrantsAllTen()
    {
        var role        = Role.CreateSystemRole(TenantId, "TenantAdmin", "Full admin", ActorId);
        var permissions = DefaultPermissions.All
            .Select(p => Permission.Create(p.Key, p.Description))
            .ToList();

        foreach (var p in permissions)
            role.GrantPermission(p, ActorId);

        role.Permissions.Should().HaveCount(10);
    }

    [Fact]
    public void GrantPermission_WhenPreviouslyRevoked_RestoresExistingRow()
    {
        var role       = Role.Create(TenantId, "TestRole", "desc", ActorId);
        var permission = Permission.Create("workflow.view", "View workflows");
        role.GrantPermission(permission, ActorId);
        role.RevokePermission(permission.Id);

        // Re-granting should restore, not add a new row.
        role.GrantPermission(permission, ActorId);

        role.Permissions.Should().HaveCount(1);
        role.Permissions[0].IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var role = Role.Create(TenantId, "OldName", "old desc", ActorId);

        role.Update("NewName", "new desc");

        role.Name.Should().Be("NewName");
        role.NormalizedName.Should().Be("NEWNAME");
        role.Description.Should().Be("new desc");
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrue()
    {
        var role = Role.Create(TenantId, "Temp", "desc", ActorId);

        role.SoftDelete();

        role.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SyncPermissions_RevokesRemovedAndGrantsNew()
    {
        var role  = Role.Create(TenantId, "TestRole", "desc", ActorId);
        var view  = Permission.Create("workflow.view",   "View");
        var create= Permission.Create("workflow.create", "Create");
        role.GrantPermission(view, ActorId);

        // Sync: drop view, add create.
        role.SyncPermissions([create], ActorId);

        role.Permissions.Single(p => p.PermissionId == view.Id).IsDeleted.Should().BeTrue();
        role.Permissions.Single(p => p.PermissionId == create.Id).IsDeleted.Should().BeFalse();
    }
}
