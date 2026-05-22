using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateTenantSettings;

public sealed record UpdateTenantSettingsCommand(
    Guid    TenantId,
    string  DisplayName,
    string  DefaultPalette,
    string  Timezone,
    string? CustomCssTokensJson,
    Guid    UpdatedBy) : IRequest<Result>;
