using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Queries.GetRole;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class GetRoleQueryHandlerTests
{
    private readonly IRoleRepository   _roles = Substitute.For<IRoleRepository>();
    private readonly GetRoleQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();

    public GetRoleQueryHandlerTests()
        => _sut = new GetRoleQueryHandler(_roles, NullLogger<GetRoleQueryHandler>.Instance);

    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsDto()
    {
        var role = Role.Create(TenantId, "Manager", "Manages things", ActorId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _sut.Handle(new GetRoleQuery(role.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(role.Id);
        result.Value.Name.Should().Be("Manager");
        result.Value.IsSystemRole.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsFailure()
    {
        _roles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.Handle(new GetRoleQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ExposesOnlyActivePermissions()
    {
        var role       = Role.Create(TenantId, "TestRole", "desc", ActorId);
        var permission = Permission.Create("workflow.view", "View");
        role.GrantPermission(permission, ActorId);
        role.RevokePermission(permission.Id); // now soft-deleted

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        var result = await _sut.Handle(new GetRoleQuery(role.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Permissions.Should().BeEmpty();
    }
}
