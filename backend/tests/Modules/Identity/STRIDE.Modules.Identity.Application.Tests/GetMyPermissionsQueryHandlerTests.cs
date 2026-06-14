using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Queries.GetMyPermissions;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class GetMyPermissionsQueryHandlerTests
{
    private readonly ICurrentUser           _currentUser   = Substitute.For<ICurrentUser>();
    private readonly ITenantContext         _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUserPermissionService _permissions   = Substitute.For<IUserPermissionService>();
    private readonly GetMyPermissionsQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    public GetMyPermissionsQueryHandlerTests()
    {
        _currentUser.UserId.Returns(UserId);
        _tenantContext.TenantId.Returns(TenantId);
        _sut = new GetMyPermissionsQueryHandler(_currentUser, _tenantContext, _permissions);
    }

    [Fact]
    public async Task Handle_ResolvesPermissionsForCurrentUserAndTenant()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal) { "workflow.view", "workflow.create" };
        _permissions
            .GetPermissionsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
            .Returns(keys);

        var result = await _sut.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo("workflow.view", "workflow.create");
        await _permissions.Received(1).GetPermissionsAsync(
            TenantId, UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoPermissions_ReturnsEmptyList()
    {
        _permissions
            .GetPermissionsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.Ordinal));

        var result = await _sut.Handle(new GetMyPermissionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
