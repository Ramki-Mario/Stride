using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using STRIDE.Modules.Identity.API.Authorization;

namespace STRIDE.Modules.Identity.API.Tests;

public sealed class PermissionAuthorizationHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();
    private const string PermissionKey = "workflow.create";

    private readonly InMemoryUserPermissionService _store = new();
    private readonly PermissionAuthorizationHandler _sut;

    public PermissionAuthorizationHandlerTests()
        => _sut = new PermissionAuthorizationHandler(_store);

    [Fact]
    public async Task Handle_WhenUserHasPermission_Succeeds()
    {
        _store.Seed(TenantId, UserId, PermissionKey, "workflow.view");
        var context = BuildContext(AuthenticatedUser(UserId, TenantId), PermissionKey);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserLacksPermission_DoesNotSucceed()
    {
        _store.Seed(TenantId, UserId, "workflow.view"); // missing workflow.create
        var context = BuildContext(AuthenticatedUser(UserId, TenantId), PermissionKey);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoPermissionsAtAll_DoesNotSucceed()
    {
        var context = BuildContext(AuthenticatedUser(UserId, TenantId), PermissionKey);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUnauthenticated_DoesNotSucceed()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity()); // no auth type → not authenticated
        var context = BuildContext(anonymous, PermissionKey);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSubClaimMissing_DoesNotSucceed()
    {
        _store.Seed(TenantId, UserId, PermissionKey);
        var identity = new ClaimsIdentity(
            [new Claim("tid", TenantId.ToString("N"))], "TestAuth"); // no sub
        var context = BuildContext(new ClaimsPrincipal(identity), PermissionKey);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTenantClaimMissing_DoesNotSucceed()
    {
        _store.Seed(TenantId, UserId, PermissionKey);
        var identity = new ClaimsIdentity(
            [new Claim("sub", UserId.ToString("N"))], "TestAuth"); // no tid
        var context = BuildContext(new ClaimsPrincipal(identity), PermissionKey);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ResolvesPermissionsScopedToTenantAndUser()
    {
        // Permission granted to a different tenant for the same user must not satisfy.
        var otherTenant = Guid.NewGuid();
        _store.Seed(otherTenant, UserId, PermissionKey);
        var context = BuildContext(AuthenticatedUser(UserId, TenantId), PermissionKey);

        await _sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ClaimsPrincipal AuthenticatedUser(Guid userId, Guid tenantId)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString("N")),
                new Claim("tid", tenantId.ToString("N")),
            ],
            authenticationType: "TestAuth")); // non-null auth type → IsAuthenticated = true

    private static AuthorizationHandlerContext BuildContext(ClaimsPrincipal user, string key)
    {
        var requirement = new PermissionRequirement(key);
        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }
}
