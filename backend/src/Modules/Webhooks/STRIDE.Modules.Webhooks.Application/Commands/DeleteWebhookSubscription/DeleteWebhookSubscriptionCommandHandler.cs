using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Domain.Exceptions;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Commands.DeleteWebhookSubscription;

internal sealed class DeleteWebhookSubscriptionCommandHandler
    : IRequestHandler<DeleteWebhookSubscriptionCommand, Result>
{
    private readonly IWebhookSubscriptionRepository _repo;
    private readonly IAuditLogger                   _audit;
    private readonly ICurrentUser                   _currentUser;

    public DeleteWebhookSubscriptionCommandHandler(
        IWebhookSubscriptionRepository repo, IAuditLogger audit, ICurrentUser currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _repo.GetByIdAsync(request.TenantId, request.SubscriptionId, cancellationToken);
        if (sub is null)
            return Result.Failure("Webhook subscription not found.");

        try
        {
            sub.Delete(request.DeletedBy);
            _repo.Update(sub);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.DeletedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.WebhookDeleted,
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
