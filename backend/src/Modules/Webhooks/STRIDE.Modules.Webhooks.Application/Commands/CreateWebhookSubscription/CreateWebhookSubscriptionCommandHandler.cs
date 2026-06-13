using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.DTOs;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Exceptions;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Commands.CreateWebhookSubscription;

internal sealed class CreateWebhookSubscriptionCommandHandler
    : IRequestHandler<CreateWebhookSubscriptionCommand, Result<WebhookSecretDto>>
{
    /// <summary>Cap per tenant to keep the dispatch fan-out (US-181) bounded.</summary>
    private const int MaxActiveSubscriptionsPerTenant = 25;

    private readonly IWebhookSubscriptionRepository _repo;
    private readonly IAuditLogger                   _audit;
    private readonly ICurrentUser                   _currentUser;

    public CreateWebhookSubscriptionCommandHandler(
        IWebhookSubscriptionRepository repo, IAuditLogger audit, ICurrentUser currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<WebhookSecretDto>> Handle(
        CreateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var activeCount = await _repo.CountActiveAsync(request.TenantId, cancellationToken);
        if (activeCount >= MaxActiveSubscriptionsPerTenant)
            return Result<WebhookSecretDto>.Failure(
                $"Maximum of {MaxActiveSubscriptionsPerTenant} webhook subscriptions reached.");

        try
        {
            var sub = WebhookSubscription.Create(
                request.TenantId, request.Url, request.EventTypes, request.CreatedBy);

            await _repo.AddAsync(sub, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.CreatedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.WebhookCreated,
                ResourceType: "WebhookSubscription",
                ResourceId:   sub.Id,
                NewValueJson: $"{{\"url\":\"{sub.Url}\"}}"));

            return Result<WebhookSecretDto>.Success(new WebhookSecretDto(sub.Id, sub.SigningSecret));
        }
        catch (WebhookDomainException ex)
        {
            return Result<WebhookSecretDto>.Failure(ex.Message);
        }
    }
}
