namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Canonical module identifier strings.  Use these constants wherever a module
/// name needs to be compared or stored (RequiresModuleAttribute, EnabledModules DB column, etc.).
/// </summary>
public static class ModuleNames
{
    public const string Workflows    = "Workflows";
    public const string Scheduling   = "Scheduling";
    public const string Reporting    = "Reporting";
    public const string Analytics    = "Analytics";
    public const string Clients      = "Clients";
    public const string Invoicing    = "Invoicing";
    public const string Teams        = "Teams";
    public const string Notifications = "Notifications";
    public const string KitOps       = "KitOps";
    // Administration, Identity, and Dashboard are always available (never gated).
}
