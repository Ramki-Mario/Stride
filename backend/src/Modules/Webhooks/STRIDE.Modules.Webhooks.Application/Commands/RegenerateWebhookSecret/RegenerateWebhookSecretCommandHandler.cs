using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.DTOs;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Commands.RegenerateWebhookSecret;

internal sealed class RegenerateWebhookSecretCommandHandler
    : IRequestHandler<RegenerateWebhookSecretCommand, Result<WebhookSecretDto>>
{
    private readonly IWebhookSubscriptionRepository _repo;
    private readonly IAuditLogger                   _audit;
    private readonly ICurrentUser                   _currentUser;

    public RegenerateWebhookSecretCommandHandler(
        IWebhookSubscriptionRepository repo, IAuditLogger audit, ICurrentUser currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<WebhookSecretDto>> Handle(
        RegenerateWebhookSecretCommand request, CancellationToken cancellationToken)
    {
        var sub = await _repo.GetByIdAsync(request.TenantId, request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result<WebhookSecretDto>.Failure("Webhook subscription not found.");

        var newSecret = sub.RegenerateSecret(request.RegeneratedBy);
        _repo.Update(sub);
        await _repo.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.RegeneratedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.WebhookSecretRegenerated,
            ResourceType: "WebhookSubscription",
            ResourceId:   sub.Id));

        return Result<WebhookSecretDto>.Success(new WebhookSecretDto(sub.Id, newSecret));
    }
}
