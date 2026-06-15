using System.Reflection;
using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Queries.GetMyRoles;
using STRIDE.Modules.Identity.Domain.Entities;
using PasswordVO = STRIDE.Modules.Identity.Domain.ValueObjects.Password;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class GetMyRolesQueryHandlerTests
{
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository    _users       = Substitute.For<IUserRepository>();
    private readonly GetMyRolesQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    public GetMyRolesQueryHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _sut = new GetMyRolesQueryHandler(_currentUser, _users);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        _users.GetByIdWithRolePermissionsAsync(UserId, Arg.Any<CancellationToken>())
              .Returns((User?)null);

        var result = await _sut.Handle(new GetMyRolesQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRoles_ReturnsEmptyList()
    {
        var user = BuildUser();
        _users.GetByIdWithRolePermissionsAsync(UserId, Arg.Any<CancellationToken>())
              .Returns(user);

        var result = await _sut.Handle(new GetMyRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeletedRoleAssignments_AreExcluded()
    {
        var user = BuildUser();
        var role = Role.Create(TenantId, "FieldWorker", "desc", ActorId);
        user.AssignRole(role, ActorId);
        user.RevokeRole(role.Id); // soft-deletes the UserRole

        _users.GetByIdWithRolePermissionsAsync(UserId, Arg.Any<CancellationToken>())
              .Returns(user);

        var result = await _sut.Handle(new GetMyRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RolesWithPermissions_MappedCorrectly()
    {
        var user = BuildUser();

        var role       = Role.Create(TenantId, "Manager", "Manages things", ActorId);
        var permission = Permission.Create("workflow.view", "View workflows");
        role.GrantPermission(permission, ActorId);

        // Set the Permission nav prop that EF Core would normally populate.
        var rp = role.Permissions.Single();
        SetPrivateField(rp, "_permission", permission);
        // RolePermission.Permission has a private setter; use backing-field name.
        SetPrivateProperty(rp, "Permission", permission);

        var userRole = SetupUserRoleWithNavProp(user, role);
        _ = userRole; // used for side-effects

        _users.GetByIdWithRolePermissionsAsync(UserId, Arg.Any<CancellationToken>())
              .Returns(user);

        var result = await _sut.Handle(new GetMyRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var dto = result.Value[0];
        dto.Name.Should().Be("Manager");
        dto.IsSystemRole.Should().BeFalse();
        dto.Permissions.Should().HaveCount(1);
        dto.Permissions[0].Key.Should().Be("workflow.view");
        dto.Permissions[0].Description.Should().Be("View workflows");
    }

    [Fact]
    public async Task Handle_MultipleRoles_ReturnedSortedByName()
    {
        var user  = BuildUser();
        var roleZ = Role.Create(TenantId, "Zebra", "Z first", ActorId);
        var roleA = Role.Create(TenantId, "Alpha", "A first", ActorId);

        SetupUserRoleWithNavProp(user, roleZ);
        SetupUserRoleWithNavProp(user, roleA);

        _users.GetByIdWithRolePermissionsAsync(UserId, Arg.Any<CancellationToken>())
              .Returns(user);

        var result = await _sut.Handle(new GetMyRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(r => r.Name).Should().ContainInOrder("Alpha", "Zebra");
    }

    [Fact]
    public async Task Handle_DeletedPermissions_AreExcluded()
    {
        var user       = BuildUser();
        var role       = Role.Create(TenantId, "Editor", "desc", ActorId);
        var permission = Permission.Create("workflow.create", "Create workflows");
        role.GrantPermission(permission, ActorId);
        role.RevokePermission(permission.Id); // soft-deletes the RolePermission

        SetupUserRoleWithNavProp(user, role);

        _users.GetByIdWithRolePermissionsAsync(UserId, Arg.Any<CancellationToken>())
              .Returns(user);

        var result = await _sut.Handle(new GetMyRolesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Permissions.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User BuildUser()
    {
        var user = User.Create(TenantId, "alice@acme.com", "Alice",
                               PasswordVO.FromHash("hash"), ActorId);
        user.ClearDomainEvents();
        return user;
    }

    /// <summary>Calls user.AssignRole and then sets the UserRole.Role nav prop via reflection.</summary>
    private static object SetupUserRoleWithNavProp(User user, Role role)
    {
        user.AssignRole(role, ActorId);
        user.ClearDomainEvents();

        var userRole = user.Roles.Single(ur => ur.RoleId == role.Id && !ur.IsDeleted);
        // UserRole.Role is a get-only auto-property; set the compiler-generated backing field.
        var field = typeof(UserRole).GetField("<Role>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(UserRole).GetField("_role",
                BindingFlags.NonPublic | BindingFlags.Instance);

        field?.SetValue(userRole, role);
        return userRole;
    }

    private static void SetPrivateProperty(object obj, string propertyName, object? value)
    {
        var prop = obj.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    private static void SetPrivateField(object obj, string fieldName, object? value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}
