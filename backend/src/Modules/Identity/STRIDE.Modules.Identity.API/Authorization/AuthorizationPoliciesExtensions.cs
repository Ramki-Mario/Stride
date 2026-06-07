using Microsoft.Extensions.DependencyInjection;
using STRIDE.Modules.Identity.Infrastructure.Persistence.SeedData;

namespace STRIDE.Modules.Identity.API.Authorization;

/// <summary>
/// Registers named RBAC authorization policies for the Identity module.
/// Called from <c>IdentityModuleExtensions.AddIdentityModule</c> so that
/// each module declares its own policy surface at composition time.
///
/// <c>AddAuthorization(options => ...)</c> is additive — calling it multiple
/// times merges policies; it does not replace previously registered ones.
/// </summary>
public static class AuthorizationPoliciesExtensions
{
    public static IServiceCollection AddIdentityAuthorizationPolicies(
        this IServiceCollection services)
    {
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
