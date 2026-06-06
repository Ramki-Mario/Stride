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
    private readonly ITenantNameResolver       _tenantNameResolver;

    public GetTenantSettingsQueryHandler(
        ITenantSettingsRepository repo,
        ITenantNameResolver       tenantNameResolver)
    {
        _repo               = repo;
        _tenantNameResolver = tenantNameResolver;
    }

    public async Task<Result<TenantSettingsDto>> Handle(
        GetTenantSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetByTenantIdAsync(request.TenantId, cancellationToken);

        if (settings is null)
        {
            // First access — resolve the tenant's registered name from the identity schema
            // so the sidebar shows the correct org name immediately after onboarding.
            var tenantName = await _tenantNameResolver.ResolveAsync(request.TenantId, cancellationToken);

            settings = TenantSettings.CreateDefaults(
                request.TenantId, request.TenantId, displayName: tenantName);

            await _repo.AddAsync(settings, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
        }
        else if (string.IsNullOrEmpty(settings.DisplayName))
        {
            // Row exists but DisplayName was never populated (created before name-resolution
            // was introduced).  Backfill it now so existing tenants get their org name.
            var tenantName = await _tenantNameResolver.ResolveAsync(request.TenantId, cancellationToken);
            if (!string.IsNullOrEmpty(tenantName))
            {
                settings.BackfillDisplayName(tenantName, request.TenantId);
                await _repo.SaveChangesAsync(cancellationToken);
            }
        }

        return Result<TenantSettingsDto>.Success(ToDto(settings));
    }

    private static TenantSettingsDto ToDto(TenantSettings s) =>
        new(s.Id, s.TenantId, s.DisplayName, s.DefaultPalette, s.Timezone, s.CustomCssTokensJson);
}
