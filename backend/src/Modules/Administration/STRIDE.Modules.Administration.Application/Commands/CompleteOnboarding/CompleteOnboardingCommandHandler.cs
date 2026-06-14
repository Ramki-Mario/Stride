using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Application.Commands.CompleteOnboarding;

internal sealed class CompleteOnboardingCommandHandler
    : IRequestHandler<CompleteOnboardingCommand, Result>
{
    private readonly ITenantSettingsRepository _repo;

    public CompleteOnboardingCommandHandler(ITenantSettingsRepository repo)
        => _repo = repo;

    public async Task<Result> Handle(
        CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetByTenantIdAsync(request.TenantId, cancellationToken);

        if (settings is null)
        {
            settings = TenantSettings.CreateDefaults(request.TenantId, request.ActorId);
            await _repo.AddAsync(settings, cancellationToken);
        }

        settings.CompleteOnboarding();
        await _repo.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
