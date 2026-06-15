using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Administration.Domain.Entities;

/// <summary>
/// Persisted per-tenant configuration: branding, localisation, and BYOT tokens.
/// One row per tenant (1:1). Created on first access if missing.
/// </summary>
public sealed class TenantSettings : AuditableEntity
{
    public string  DisplayName          { get; private set; } = string.Empty;
    public string  DefaultPalette       { get; private set; } = "purple";
    public string  Timezone             { get; private set; } = "UTC";
    public string? CustomCssTokensJson  { get; private set; }
    public bool    OnboardingCompleted  { get; private set; }
    /// <summary>
    /// Comma-separated list of enabled module identifiers (e.g. "KitOps,Reporting").
    /// Null or empty means ALL modules are enabled — default for existing and new tenants.
    /// </summary>
    public string? EnabledModules       { get; private set; }

    private TenantSettings() { }   // EF

    public static TenantSettings CreateDefaults(Guid tenantId, Guid createdBy, string displayName = "")
    {
        var now = DateTime.UtcNow;
        return new TenantSettings
        {
            Id                  = Guid.NewGuid(),
            TenantId            = tenantId,
            DisplayName         = displayName,
            DefaultPalette      = "purple",
            Timezone            = "UTC",
            CustomCssTokensJson = null,
            OnboardingCompleted = false,
            CreatedAt           = now,
            UpdatedAt           = now,
            CreatedBy           = createdBy,
        };
    }

    public void CompleteOnboarding()
    {
        OnboardingCompleted = true;
        UpdatedAt           = DateTime.UtcNow;
    }

    /// <summary>
    /// One-time backfill: sets DisplayName from the identity schema when the row
    /// was originally created before name-resolution was introduced.
    /// Only call when DisplayName is currently empty.
    /// </summary>
    public void BackfillDisplayName(string displayName, Guid updatedBy)
    {
        DisplayName = displayName;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void Update(
        string  displayName,
        string  defaultPalette,
        string  timezone,
        string? customCssTokensJson,
        Guid    updatedBy)
    {
        DisplayName         = displayName;
        DefaultPalette      = defaultPalette;
        Timezone            = timezone;
        CustomCssTokensJson = customCssTokensJson;
        UpdatedAt           = DateTime.UtcNow;
    }

    /// <summary>
    /// Replaces the enabled-module list. Pass an empty/null collection to grant all modules.
    /// </summary>
    public void SetEnabledModules(IEnumerable<string>? modules)
    {
        var list = modules?.Select(m => m.Trim()).Where(m => m.Length > 0).ToList();
        EnabledModules = list is { Count: > 0 } ? string.Join(",", list) : null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true when the module is enabled for this tenant.
    /// Null/empty EnabledModules means all modules are enabled (backward-compatible default).
    /// </summary>
    public bool IsModuleEnabled(string moduleName) =>
        string.IsNullOrEmpty(EnabledModules) ||
        EnabledModules.Split(',').Any(m => m.Equals(moduleName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Parses EnabledModules into a list. Empty list means all modules are enabled.</summary>
    public IReadOnlyList<string> GetEnabledModulesList() =>
        string.IsNullOrEmpty(EnabledModules)
            ? Array.Empty<string>()
            : EnabledModules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
