namespace STRIDE.Modules.Identity.API.Authorization;

/// <summary>
/// Named authorization policy constants for STRIDE.
///
/// Use with <c>[Authorize(Policy = Policies.RequireAdmin)]</c> on controllers
/// or actions. Policies are registered by
/// <see cref="AuthorizationPoliciesExtensions.AddIdentityAuthorizationPolicies"/>.
///
/// Policy → role membership:
/// <list type="table">
///   <item><term>RequireAuthenticated</term><description>Any valid JWT — no role required</description></item>
///   <item><term>RequireAdmin</term><description>Admin</description></item>
///   <item><term>RequireManager</term><description>Admin | OperationsManager</description></item>
///   <item><term>RequireFinance</term><description>Admin | FinanceUser</description></item>
///   <item><term>RequireSupervisor</term><description>Admin | OperationsManager | Supervisor</description></item>
///   <item><term>RequireFieldWorker</term><description>Admin | FieldWorker</description></item>
/// </list>
/// </summary>
public static class Policies
{
    /// <summary>Any authenticated user — no role check.</summary>
    public const string RequireAuthenticated = "RequireAuthenticated";

    /// <summary>Admin role only — full system access, user provisioning.</summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>Admin or OperationsManager — workflow and scheduling management.</summary>
    public const string RequireManager = "RequireManager";

    /// <summary>Admin or FinanceUser — financial reporting and invoicing.</summary>
    public const string RequireFinance = "RequireFinance";

    /// <summary>Admin, OperationsManager, or Supervisor — team oversight and approvals.</summary>
    public const string RequireSupervisor = "RequireSupervisor";

    /// <summary>Admin or FieldWorker — field task execution.</summary>
    public const string RequireFieldWorker = "RequireFieldWorker";
}
