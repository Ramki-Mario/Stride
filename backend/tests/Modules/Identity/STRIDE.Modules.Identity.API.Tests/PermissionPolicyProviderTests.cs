using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using STRIDE.Modules.Identity.API.Authorization;
using STRIDE.Modules.Identity.Domain;

namespace STRIDE.Modules.Identity.API.Tests;

public sealed class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider BuildProvider(Action<AuthorizationOptions>? configure = null)
    {
        var options = new AuthorizationOptions();
        configure?.Invoke(options);
        return new PermissionPolicyProvider(Options.Create(options));
    }

    [Fact]
    public async Task GetPolicyAsync_ForCatalogKey_BuildsPermissionPolicy()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync(DefaultPermissions.WorkflowCreate);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle(r =>
            r is PermissionRequirement && ((PermissionRequirement)r).PermissionKey == DefaultPermissions.WorkflowCreate);
    }

    [Fact]
    public async Task GetPolicyAsync_ForCatalogKey_RequiresAuthenticatedUser()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync(DefaultPermissions.TenantSettings);

        // RequireAuthenticatedUser() adds a DenyAnonymousAuthorizationRequirement so that
        // anonymous callers get a 401 challenge before the permission handler runs.
        policy!.Requirements.Should().Contain(r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Theory]
    [InlineData("workflow.view")]
    [InlineData("user.manage")]
    [InlineData("role.view")]
    [InlineData("tenant.settings")]
    public async Task GetPolicyAsync_ForEveryCatalogKey_ReturnsPermissionPolicy(string key)
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync(key);

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>()
            .Should().ContainSingle(r => r.PermissionKey == key);
    }

    [Fact]
    public async Task GetPolicyAsync_ForLegacyNamedPolicy_DelegatesToFallback()
    {
        var provider = BuildProvider(opts =>
            opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin")));

        var policy = await provider.GetPolicyAsync("RequireAdmin");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().NotContain(r => r is PermissionRequirement);
        policy.Requirements.Should().Contain(r => r is RolesAuthorizationRequirement);
    }

    [Fact]
    public async Task GetPolicyAsync_ForUnknownPolicy_ReturnsNull()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync("does.not.exist");

        policy.Should().BeNull();
    }
}
