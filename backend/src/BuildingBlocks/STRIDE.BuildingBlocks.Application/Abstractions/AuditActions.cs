namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// String constants for audit log action names.
/// Lives in BuildingBlocks so every module can reference them without
/// cross-module dependencies. String constants (not enums) keep values
/// human-readable in the database and require no migration for new actions.
/// </summary>
public static class AuditActions
{
    // ── User management ───────────────────────────────────────────────────────
    public const string UserInvited     = "user.invited";
    public const string UserRoleChanged = "user.role_changed";
    public const string UserDeactivated = "user.deactivated";
    public const string UserReactivated = "user.reactivated";

    // ── Invoicing ─────────────────────────────────────────────────────────────
    public const string InvoiceGenerated = "invoice.generated";
    public const string InvoiceSent      = "invoice.sent";
    public const string InvoicePaid      = "invoice.paid";
    public const string InvoiceVoided    = "invoice.voided";

    // ── Tenant settings ───────────────────────────────────────────────────────
    public const string TenantSettingsUpdated = "tenant_settings.updated";
}
