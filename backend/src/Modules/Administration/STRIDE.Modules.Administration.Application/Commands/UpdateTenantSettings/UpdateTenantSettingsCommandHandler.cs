using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateTenantSettings;

internal sealed class UpdateTenantSettingsCommandHandler
    : IRequestHandler<UpdateTenantSettingsCommand, Result>
{
    private readonly ITenantSettingsRepository _repo;

    public UpdateTenantSettingsCommandHandler(ITenantSettingsRepository repo) => _repo = repo;

    public async Task<Result> Handle(UpdateTenantSettingsCommand request, CancellationToken ct)
    {
        var settings = await _repo.GetByTenantIdAsync(request.TenantId, ct);

        if (settings is null)
        {
            settings = TenantSettings.CreateDefaults(request.TenantId, request.UpdatedBy);
            await _repo.AddAsync(settings, ct);
        }

        settings.Update(
            request.DisplayName,
            request.DefaultPalette,
            request.Timezone,
            request.CustomCssTokensJson,
            request.UpdatedBy);

        await _repo.SaveChangesAsync(ct);
        return Result.Success();
    }
}
