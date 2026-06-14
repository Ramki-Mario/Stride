using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.UpdateRole;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class UpdateRoleCommandHandlerTests
{
    private readonly IRoleRepository        _roles     = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository  _perms     = Substitute.For<IPermissionRepository>();
    private readonly IUserPermissionService _userPerms = Substitute.For<IUserPermissionService>();
    private readonly ITenantContext         _tenant    = Substitute.For<ITenantContext>();

    private readonly UpdateRoleCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();

    private static readonly Permission WorkflowView =
        Permission.Create("workflow.view", "View workflows");

    public UpdateRoleCommandHandlerTests()
    {
        _tenant.TenantId.Returns(TenantId);
        _sut = new UpdateRoleCommandHandler(
            _roles, _perms, _userPerms, _tenant,
            NullLogger<UpdateRoleCommandHandler>.Instance);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidUpdate_SavesAndInvalidatesCache()
    {
        var role = Role.Create(TenantId, "OldName", "old", ActorId);
        SetupRole(role);
        SetupAllPermissions();
        SetupActorHasAllPermissions("workflow.view");
        _roles.GetAssignedUserIdsAsync(role.Id, Arg.Any<CancellationToken>())
              .Returns(new[] { ActorId } as IReadOnlyList<Guid>);

        var result = await _sut.Handle(
            new UpdateRoleCommand(role.Id, "NewName", "new desc", [WorkflowView.Id], ActorId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _roles.Received(1).Update(Arg.Is<Role>(r => r.Name == "NewName"));
        _userPerms.Received(1).InvalidateUser(TenantId, ActorId);
    }

    // ── Guard: role not found ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsFailure()
    {
        _roles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.Handle(
            new UpdateRoleCommand(Guid.NewGuid(), "Name", "desc", [], ActorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Guard: system role ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenSystemRole_ReturnsFailure()
    {
        var system = Role.CreateSystemRole(TenantId, "TenantAdmin", "Full admin", ActorId);
        SetupRole(system);

        var result = await _sut.Handle(
            new UpdateRoleCommand(system.Id, "TenantAdmin", "desc", [], ActorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("System roles cannot be modified");
    }

    // ── Guard: no-escalation ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenActorLacksPermission_ReturnsFailure()
    {
        var role = Role.Create(TenantId, "Manager", "desc", ActorId);
        SetupRole(role);
        SetupAllPermissions();
        // Actor has no permissions
        _userPerms
            .GetPermissionsAsync(TenantId, ActorId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string>(StringComparer.Ordinal));

        var result = await _sut.Handle(
            new UpdateRoleCommand(role.Id, "Manager", "desc", [WorkflowView.Id], ActorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("workflow.view");
    }

    // ── Guard: duplicate name ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRenamedToExistingName_ReturnsFailure()
    {
        var role = Role.Create(TenantId, "Manager", "desc", ActorId);
        SetupRole(role);
        SetupAllPermissions();
        // A *different* role already has the name "Supervisor"
        _roles.ExistsByNameAsync("SUPERVISOR", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(
            new UpdateRoleCommand(role.Id, "Supervisor", "desc", [], ActorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupRole(Role role)
        => _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

    private void SetupAllPermissions()
        => _perms.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { WorkflowView } as IReadOnlyList<Permission>);

    private void SetupActorHasAllPermissions(params string[] keys)
        => _userPerms
            .GetPermissionsAsync(TenantId, ActorId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string>(keys, StringComparer.Ordinal));
}
