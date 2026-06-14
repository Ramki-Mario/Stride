using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.API.Authorization;

/// <summary>
/// Evaluates a <see cref="PermissionRequirement"/> against the current user's effective
/// permission set, resolved dynamically from the tenant's role configuration (US-134).
///
/// The user (sub) and tenant (tid) are read from the validated JWT claims. The permission
/// set is resolved + cached by <see cref="IUserPermissionService"/>. An authenticated user
/// who lacks the permission simply fails the requirement, which ASP.NET Core surfaces as
/// 403 Forbidden; an unauthenticated request is rejected as 401 by the policy's
/// <c>RequireAuthenticatedUser()</c> clause before this handler is consulted.
/// </summary>
internal sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionService _permissions;

    public PermissionAuthorizationHandler(IUserPermissionService permissions)
        => _permissions = permissions;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return; // Unauthenticated — leave unmet; pipeline issues a 401 challenge.

        if (!TryGetGuid(context.User, "sub", out var userId) ||
            !TryGetGuid(context.User, "tid", out var tenantId))
            return; // Malformed token — deny.

        var granted = await _permissions.GetPermissionsAsync(tenantId, userId);

        if (granted.Contains(requirement.PermissionKey))
            context.Succeed(requirement);
    }

    private static bool TryGetGuid(ClaimsPrincipal user, string claimType, out Guid value)
        => Guid.TryParse(user.FindFirstValue(claimType), out value);
}
