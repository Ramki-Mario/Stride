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

    // ── Clients ───────────────────────────────────────────────────────────────
    public const string ClientCreated     = "client.created";
    public const string ClientUpdated     = "client.updated";
    public const string ClientDeactivated = "client.deactivated";
    public const string ClientReactivated = "client.reactivated";

    // ── Workflows ─────────────────────────────────────────────────────────────
    public const string WorkflowTeamAssigned = "workflow.team_assigned";

    // ── Tenant settings ───────────────────────────────────────────────────────
    public const string TenantSettingsUpdated = "tenant_settings.updated";

    // ── Teams ─────────────────────────────────────────────────────────────────
    public const string TeamCreated     = "team.created";
    public const string TeamUpdated     = "team.updated";
    public const string TeamDeactivated = "team.deactivated";
    public const string TeamReactivated = "team.reactivated";

    // ── Webhooks ──────────────────────────────────────────────────────────────
    public const string WebhookCreated          = "webhook.created";
    public const string WebhookUpdated          = "webhook.updated";
    public const string WebhookDeleted          = "webhook.deleted";
    public const string WebhookSecretRegenerated = "webhook.secret_regenerated";

    // ── KitOps ────────────────────────────────────────────────────────────────
    public const string KitItemCreated     = "kititem.created";
    public const string KitItemUpdated     = "kititem.updated";
    public const string KitItemDeactivated = "kititem.deactivated";
    public const string KitItemReactivated = "kititem.reactivated";
}
