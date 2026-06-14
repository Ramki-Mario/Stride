using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.CreateRole;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class CreateRoleCommandHandlerTests
{
    private readonly IRoleRepository        _roles       = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository  _perms       = Substitute.For<IPermissionRepository>();
    private readonly IUserPermissionService _userPerms   = Substitute.For<IUserPermissionService>();
    private readonly ITenantContext         _tenant      = Substitute.For<ITenantContext>();

    private readonly CreateRoleCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();

    private static readonly Permission WorkflowView =
        Permission.Create("workflow.view", "View workflows");

    private static readonly Permission WorkflowCreate =
        Permission.Create("workflow.create", "Create workflows");

    public CreateRoleCommandHandlerTests()
    {
        _tenant.TenantId.Returns(TenantId);
        _sut = new CreateRoleCommandHandler(
            _roles, _perms, _userPerms, _tenant,
            NullLogger<CreateRoleCommandHandler>.Instance);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_CreatesRoleAndReturnsId()
    {
        SetupNoDuplicate();
        SetupAllPermissions();
        SetupActorHasAllPermissions("workflow.view", "workflow.create");

        var result = await _sut.Handle(
            new CreateRoleCommand("Manager", "Manages stuff", [WorkflowView.Id, WorkflowCreate.Id], ActorId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _roles.Received(1).AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await _roles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoPermissions_Succeeds()
    {
        SetupNoDuplicate();
        SetupAllPermissions();

        var result = await _sut.Handle(
            new CreateRoleCommand("ReadOnly", "desc", [], ActorId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _userPerms.DidNotReceive()
            .GetPermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Guard: duplicate name ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ReturnsFailure()
    {
        _roles.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(
            new CreateRoleCommand("Manager", "desc", [], ActorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        await _roles.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    // ── Guard: no-escalation ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenActorLacksRequestedPermission_ReturnsFailure()
    {
        SetupNoDuplicate();
        SetupAllPermissions();
        // Actor only has workflow.view — NOT workflow.create
        SetupActorHasAllPermissions("workflow.view");

        var result = await _sut.Handle(
            new CreateRoleCommand("Manager", "desc", [WorkflowCreate.Id], ActorId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("workflow.create");
        await _roles.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenActorHasAllRequestedPermissions_Succeeds()
    {
        SetupNoDuplicate();
        SetupAllPermissions();
        SetupActorHasAllPermissions("workflow.view");

        var result = await _sut.Handle(
            new CreateRoleCommand("Viewer", "desc", [WorkflowView.Id], ActorId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupNoDuplicate()
        => _roles.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

    private void SetupAllPermissions()
        => _perms.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { WorkflowView, WorkflowCreate } as IReadOnlyList<Permission>);

    private void SetupActorHasAllPermissions(params string[] keys)
        => _userPerms
            .GetPermissionsAsync(TenantId, ActorId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<string>)new HashSet<string>(keys, StringComparer.Ordinal));
}
