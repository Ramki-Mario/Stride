using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Identity.Infrastructure.Persistence.SeedData;

namespace STRIDE.Modules.Identity.API.Authorization;

/// <summary>
/// Registers RBAC authorization for the Identity module:
///   1. The dynamic permission pipeline (US-134) — <see cref="PermissionPolicyProvider"/>
///      synthesises a policy per permission key, satisfied by
///      <see cref="PermissionAuthorizationHandler"/> against the tenant's role config.
///   2. The legacy named role policies, kept as a fallback for any callers still using them.
///
/// Called from <c>IdentityModuleExtensions.AddIdentityModule</c> so that the module declares
/// its own policy surface at composition time. <c>AddAuthorizationBuilder()</c> is additive.
/// </summary>
public static class AuthorizationPoliciesExtensions
{
    public static IServiceCollection AddIdentityAuthorizationPolicies(
        this IServiceCollection services)
    {
        // ── Dynamic permission authorization (US-134) ─────────────────────────
        // Permission sets are resolved + cached in-process, so a memory cache is required.
        services.AddMemoryCache();

        // The policy provider must be a singleton (ASP.NET resolves it once). The handler
        // is scoped because it depends on the scoped IUserPermissionService (DbContext).
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // ── Legacy named role policies (fallback / backward compatibility) ────
        services.AddAuthorizationBuilder()
            // ── Tier 0: any authenticated user ────────────────────────────
            .AddPolicy(Policies.RequireAuthenticated,
                policy => policy.RequireAuthenticatedUser())

            // ── Tier 1: Admin-only ─────────────────────────────────────────
            .AddPolicy(Policies.RequireAdmin,
                policy => policy.RequireRole(DefaultRoles.Admin))

            // ── Tier 2: Admin OR OperationsManager ────────────────────────
            .AddPolicy(Policies.RequireManager,
                policy => policy.RequireRole(
                    DefaultRoles.Admin,
                    DefaultRoles.OperationsManager))

            // ── Tier 3: Admin OR FinanceUser ──────────────────────────────
            .AddPolicy(Policies.RequireFinance,
                policy => policy.RequireRole(
                    DefaultRoles.Admin,
                    DefaultRoles.FinanceUser))

            // ── Tier 4: Admin, OperationsManager, OR Supervisor ───────────
            .AddPolicy(Policies.RequireSupervisor,
                policy => policy.RequireRole(
                    DefaultRoles.Admin,
                    DefaultRoles.OperationsManager,
                    DefaultRoles.Supervisor))

            // ── Tier 5: Admin OR FieldWorker ──────────────────────────────
            .AddPolicy(Policies.RequireFieldWorker,
                policy => policy.RequireRole(
                    DefaultRoles.Admin,
                    DefaultRoles.FieldWorker));

        return services;
    }
}
