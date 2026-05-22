using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.DTOs;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateTenantSettings;

/// <summary>
/// Updates tenant settings. If <see cref="CustomCss"/> is provided it is sanitised
/// server-side before storage and the sanitisation result is returned so the caller
/// can surface per-line feedback to the user.
/// </summary>
public sealed record UpdateTenantSettingsCommand(
    Guid    TenantId,
    string  DisplayName,
    string  DefaultPalette,
    string  Timezone,
    /// <summary>Raw CSS text from the user. Null = keep existing tokens unchanged.</summary>
    string? CustomCss,
    Guid    UpdatedBy) : IRequest<Result<SanitisedCssResult?>>;
