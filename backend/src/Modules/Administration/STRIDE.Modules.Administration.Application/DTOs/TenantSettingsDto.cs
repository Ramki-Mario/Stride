namespace STRIDE.Modules.Administration.Application.DTOs;

public sealed record TenantSettingsDto(
    Guid    Id,
    Guid    TenantId,
    string  DisplayName,
    string  DefaultPalette,
    string  Timezone,
    string? CustomCssTokensJson);
