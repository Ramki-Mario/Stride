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

    public string? CustomCssTokensJson { get; set; }
}
