using System.ComponentModel.DataAnnotations;

namespace STRIDE.Modules.Administration.API.DTOs;

public sealed class UpdateTenantSettingsRequest
{
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string DefaultPalette { get; set; } = "purple";

    [Required, MaxLength(100)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Raw CSS text from the client. When provided, it is sanitised server-side
    /// and only <c>--stride-*</c> tokens inside <c>:root {}</c> are stored.
    /// Null means "keep existing CSS tokens unchanged".
    /// </summary>
    [MaxLength(100_000)]
    public string? CustomCss { get; set; }

    /// <summary>
    /// Module entitlement list. Null = leave unchanged. Empty list = enable all modules.
    /// Provide module names from <c>ModuleNames</c> constants (e.g. "KitOps", "Workflows").
    /// </summary>
    public List<string>? EnabledModules { get; set; }
}
