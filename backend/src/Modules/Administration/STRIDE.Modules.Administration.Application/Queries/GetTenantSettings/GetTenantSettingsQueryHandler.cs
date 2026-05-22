using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Application.DTOs;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Application.Queries.GetTenantSettings;

internal sealed class GetTenantSettingsQueryHandler
    : IRequestHandler<GetTenantSettingsQuery, Result<TenantSettingsDto>>
{
    private readonly ITenantSettingsRepository _repo;

    public GetTenantSettingsQueryHandler(ITenantSettingsRepository repo) => _repo = repo;

    public async Task<Result<TenantSettingsDto>> Handle(
        GetTenantSettingsQuery request, CancellationToken ct)
    {
        var settings = await _repo.GetByTenantIdAsync(request.TenantId, ct);

        if (settings is null)
        {
            // Create defaults on first access — every tenant has settings.
            settings = TenantSettings.CreateDefaults(request.TenantId, request.TenantId);
            await _repo.AddAsync(settings, ct);
            await _repo.SaveChangesAsync(ct);
        }

        return Result<TenantSettingsDto>.Success(ToDto(settings));
    }

    private static TenantSettingsDto ToDto(TenantSettings s) =>
        new(s.Id, s.TenantId, s.DisplayName, s.DefaultPalette, s.Timezone, s.CustomCssTokensJson);
}
