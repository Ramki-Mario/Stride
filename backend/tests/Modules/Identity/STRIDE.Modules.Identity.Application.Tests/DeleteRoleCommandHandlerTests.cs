using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.DeleteRole;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class DeleteRoleCommandHandlerTests
{
    private readonly IRoleRepository        _roles     = Substitute.For<IRoleRepository>();
    private readonly IUserPermissionService _userPerms = Substitute.For<IUserPermissionService>();
    private readonly ITenantContext         _tenant    = Substitute.For<ITenantContext>();

    private readonly DeleteRoleCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();

    public DeleteRoleCommandHandlerTests()
    {
        _tenant.TenantId.Returns(TenantId);
        _sut = new DeleteRoleCommandHandler(
            _roles, _userPerms, _tenant,
            NullLogger<DeleteRoleCommandHandler>.Instance);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidDelete_SoftDeletesRoleAndInvalidatesCache()
    {
        var user = Guid.NewGuid();
        var role = Role.Create(TenantId, "Temp", "desc", ActorId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _roles.GetAssignedUserIdsAsync(role.Id, Arg.Any<CancellationToken>())
              .Returns(new[] { user } as IReadOnlyList<Guid>);

        var result = await _sut.Handle(
            new DeleteRoleCommand(role.Id, ActorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        role.IsDeleted.Should().BeTrue();
        _roles.Received(1).Update(role);
        _userPerms.Received(1).InvalidateUser(TenantId, user);
    }

    [Fact]
    public async Task Handle_WhenNoUsersAssigned_SucceedsWithoutCacheInvalidation()
    {
        var role = Role.Create(TenantId, "Unused", "desc", ActorId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _roles.GetAssignedUserIdsAsync(role.Id, Arg.Any<CancellationToken>())
              .Returns(Array.Empty<Guid>() as IReadOnlyList<Guid>);

        var result = await _sut.Handle(
            new DeleteRoleCommand(role.Id, ActorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userPerms.DidNotReceive().InvalidateUser(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    // ── Guard: role not found ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsFailure()
    {
        _roles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.Handle(
            new DeleteRoleCommand(Guid.NewGuid(), ActorId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Guard: system role ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenSystemRole_ReturnsFailure()
    {
        var system = Role.CreateSystemRole(TenantId, "TenantAdmin", "Full admin", ActorId);
        _roles.GetByIdAsync(system.Id, Arg.Any<CancellationToken>()).Returns(system);

        var result = await _sut.Handle(
            new DeleteRoleCommand(system.Id, ActorId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("System roles cannot be deleted");
        _roles.DidNotReceive().Update(Arg.Any<Role>());
    }
}
