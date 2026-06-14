namespace STRIDE.Modules.Identity.Domain;

/// <summary>
/// System-wide permission key catalog. These keys are seeded once via data migration
/// and referenced by tenant roles. Add new keys here + a new migration when extending the catalog.
/// </summary>
public static class DefaultPermissions
{
    public const string WorkflowView            = "workflow.view";
    public const string WorkflowCreate          = "workflow.create";
    public const string WorkflowActivate        = "workflow.activate";
    public const string WorkflowRun             = "workflow.run";
    public const string WorkflowManageInstances = "workflow.manage_instances";
    public const string UserInvite              = "user.invite";
    public const string UserManage              = "user.manage";
    public const string RoleView                = "role.view";
    public const string RoleManage              = "role.manage";
    public const string TenantSettings          = "tenant.settings";

    /// <summary>All permissions in (Key, Description, seeded Id) order. Ids are stable — never change them.</summary>
    public static readonly IReadOnlyList<(Guid Id, string Key, string Description)> All =
    [
        (new Guid("00000001-0000-0000-0000-000000000000"), WorkflowView,            "View workflow definitions"),
        (new Guid("00000002-0000-0000-0000-000000000000"), WorkflowCreate,          "Create and edit workflow definitions"),
        (new Guid("00000003-0000-0000-0000-000000000000"), WorkflowActivate,        "Activate or archive workflows"),
        (new Guid("00000004-0000-0000-0000-000000000000"), WorkflowRun,             "Start a workflow instance"),
        (new Guid("00000005-0000-0000-0000-000000000000"), WorkflowManageInstances, "View and cancel any workflow instance"),
        (new Guid("00000006-0000-0000-0000-000000000000"), UserInvite,              "Invite new users to the tenant"),
        (new Guid("00000007-0000-0000-0000-000000000000"), UserManage,              "Edit user roles and deactivate accounts"),
        (new Guid("00000008-0000-0000-0000-000000000000"), RoleView,                "View the tenant role catalog"),
        (new Guid("00000009-0000-0000-0000-000000000000"), RoleManage,              "Create, edit, and delete tenant roles"),
        (new Guid("0000000a-0000-0000-0000-000000000000"), TenantSettings,          "Edit tenant-level settings"),
    ];
}
