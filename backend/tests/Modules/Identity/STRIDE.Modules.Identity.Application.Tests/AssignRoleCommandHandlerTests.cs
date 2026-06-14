using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.AssignRole;
using STRIDE.Modules.Identity.Domain.Entities;
using PasswordVO = STRIDE.Modules.Identity.Domain.ValueObjects.Password;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class AssignRoleCommandHandlerTests
{
    private readonly IUserRepository        _users       = Substitute.For<IUserRepository>();
    private readonly IRoleRepository        _roles       = Substitute.For<IRoleRepository>();
    private readonly IUserPermissionService _permissions = Substitute.For<IUserPermissionService>();

    private readonly AssignRoleCommandHandler _sut;

    private static readonly Guid TenantId   = Guid.NewGuid();
    private static readonly Guid AssignedBy = Guid.NewGuid();

    public AssignRoleCommandHandlerTests()
        => _sut = new AssignRoleCommandHandler(
            _users, _roles, _permissions, NullLogger<AssignRoleCommandHandler>.Instance);

    [Fact]
    public async Task Handle_OnSuccess_InvalidatesPermissionCacheForUser()
    {
        var user = BuildUser();
        var role = Role.Create(TenantId, "FieldWorker", "desc", AssignedBy);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _sut.Handle(
            new AssignRoleCommand(user.Id, role.Id, AssignedBy), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _permissions.Received(1).InvalidateUser(TenantId, user.Id);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_DoesNotInvalidateCache()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.Handle(
            new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), AssignedBy), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _permissions.DidNotReceive().InvalidateUser(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_DoesNotInvalidateCache()
    {
        var user = BuildUser();
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.Handle(
            new AssignRoleCommand(user.Id, Guid.NewGuid(), AssignedBy), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _permissions.DidNotReceive().InvalidateUser(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    private static User BuildUser()
    {
        var user = User.Create(TenantId, "bob@acme.com", "Bob", PasswordVO.FromHash("hash"), AssignedBy);
        user.ClearDomainEvents();
        return user;
    }
}
