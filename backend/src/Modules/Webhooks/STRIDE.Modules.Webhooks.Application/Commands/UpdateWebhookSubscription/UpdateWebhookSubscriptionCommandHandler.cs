using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Domain.Exceptions;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Commands.UpdateWebhookSubscription;

internal sealed class UpdateWebhookSubscriptionCommandHandler
    : IRequestHandler<UpdateWebhookSubscriptionCommand, Result>
{
    private readonly IWebhookSubscriptionRepository _repo;
    private readonly IAuditLogger                   _audit;
    private readonly ICurrentUser                   _currentUser;

    public UpdateWebhookSubscriptionCommandHandler(
        IWebhookSubscriptionRepository repo, IAuditLogger audit, ICurrentUser currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _repo.GetByIdAsync(request.TenantId, request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result.Failure("Webhook subscription not found.");

        try
        {
            sub.Update(request.Url, request.EventTypes, request.IsActive, request.UpdatedBy);
            _repo.Update(sub);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.UpdatedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.WebhookUpdated,
                ResourceType: "WebhookSubscription",
                ResourceId:   sub.Id));

            return Result.Success();
        }
        catch (WebhookDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
