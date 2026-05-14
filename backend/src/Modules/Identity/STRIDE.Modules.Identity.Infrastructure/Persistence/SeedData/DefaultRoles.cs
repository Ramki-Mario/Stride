namespace STRIDE.Modules.Identity.Infrastructure.Persistence.SeedData;

/// <summary>
/// System-defined role names provisioned for every new tenant during onboarding.
/// These constants are used by the tenant provisioning service to seed roles
/// rather than hard-coding them in a migration (which would require a fixed TenantId).
/// </summary>
public static class DefaultRoles
{
    public const string Admin = "Admin";
    public const string OperationsManager = "OperationsManager";
    public const string FinanceUser = "FinanceUser";
    public const string FieldWorker = "FieldWorker";
    public const string Supervisor = "Supervisor";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Admin,
        OperationsManager,
        FinanceUser,
        FieldWorker,
        Supervisor
    };
}
