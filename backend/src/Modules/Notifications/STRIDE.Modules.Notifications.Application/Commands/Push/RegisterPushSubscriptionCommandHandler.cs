using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Application.Commands.Push;

internal sealed class RegisterPushSubscriptionCommandHandler
    : IRequestHandler<RegisterPushSubscriptionCommand, Result>
{
    private readonly IPushSubscriptionRepository _pushSubs;

    public RegisterPushSubscriptionCommandHandler(IPushSubscriptionRepository pushSubs)
    {
        _pushSubs = pushSubs;
    }

    public async Task<Result> Handle(RegisterPushSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: if the same endpoint is already registered, do nothing.
        var existing = await _pushSubs.GetByEndpointAsync(request.TenantId, request.Endpoint, cancellationToken);
        if (existing is not null)
            return Result.Success();

        var subscription = PushSubscription.Create(
            request.TenantId,
            request.UserId,
            request.Endpoint,
            request.P256dh,
            request.Auth);

        await _pushSubs.AddAsync(subscription, cancellationToken);
        await _pushSubs.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
