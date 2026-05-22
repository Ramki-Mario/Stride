using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Administration.Domain.Entities;

/// <summary>
/// Persisted per-tenant configuration: branding, localisation, and BYOT tokens.
/// One row per tenant (1:1). Created on first access if missing.
/// </summary>
public sealed class TenantSettings : AuditableEntity
{
    public string  DisplayName         { get; private set; } = string.Empty;
    public string  DefaultPalette      { get; private set; } = "purple";
    public string  Timezone            { get; private set; } = "UTC";
    public string? CustomCssTokensJson { get; private set; }

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
            CreatedAt           = now,
            UpdatedAt           = now,
            CreatedBy           = createdBy,
        };
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
}
