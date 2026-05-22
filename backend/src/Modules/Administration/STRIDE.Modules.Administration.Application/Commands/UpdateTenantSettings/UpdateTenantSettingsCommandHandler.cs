using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Application.DTOs;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateTenantSettings;

internal sealed class UpdateTenantSettingsCommandHandler
    : IRequestHandler<UpdateTenantSettingsCommand, Result<SanitisedCssResult?>>
{
    private readonly ITenantSettingsRepository _repo;
    private readonly ICssSanitiser             _sanitiser;

    public UpdateTenantSettingsCommandHandler(
        ITenantSettingsRepository repo,
        ICssSanitiser             sanitiser)
    {
        _repo      = repo;
        _sanitiser = sanitiser;
    }

    public async Task<Result<SanitisedCssResult?>> Handle(
        UpdateTenantSettingsCommand request, CancellationToken ct)
    {
        var settings = await _repo.GetByTenantIdAsync(request.TenantId, ct);

        if (settings is null)
        {
            settings = TenantSettings.CreateDefaults(request.TenantId, request.UpdatedBy);
            await _repo.AddAsync(settings, ct);
        }

        // Sanitise CSS if provided; otherwise preserve existing tokens.
        SanitisedCssResult? sanitisedResult = null;
        string? cssTokensJson = null;   // null means "leave unchanged"

        if (request.CustomCss is not null)
        {
            sanitisedResult = _sanitiser.Sanitise(request.CustomCss);
            cssTokensJson   = sanitisedResult.AcceptedTokens.Count > 0
                ? sanitisedResult.ToJson()
                : null;
        }

        settings.Update(
            request.DisplayName,
            request.DefaultPalette,
            request.Timezone,
            request.CustomCss is not null ? cssTokensJson : settings.CustomCssTokensJson,
            request.UpdatedBy);

        await _repo.SaveChangesAsync(ct);
        return Result<SanitisedCssResult?>.Success(sanitisedResult);
    }
}
