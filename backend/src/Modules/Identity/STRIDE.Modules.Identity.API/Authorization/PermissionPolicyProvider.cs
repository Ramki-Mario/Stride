using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using STRIDE.Modules.Identity.Domain;

namespace STRIDE.Modules.Identity.API.Authorization;

/// <summary>
/// Dynamic authorization policy provider (US-134). For any policy name that matches a
/// key in the system permission catalog (<see cref="DefaultPermissions"/>), a policy is
/// synthesised on demand requiring an authenticated user plus the matching
/// <see cref="PermissionRequirement"/>. This removes the need to register a named policy
/// per permission key.
///
/// All other policy names (the legacy <c>RequireAdmin</c>/<c>RequireManager</c> set, the
/// default policy, and the fallback policy) are delegated to the framework's
/// <see cref="DefaultAuthorizationPolicyProvider"/>.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private static readonly HashSet<string> PermissionKeys =
        DefaultPermissions.All.Select(p => p.Key).ToHashSet(StringComparer.Ordinal);

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (PermissionKeys.Contains(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
