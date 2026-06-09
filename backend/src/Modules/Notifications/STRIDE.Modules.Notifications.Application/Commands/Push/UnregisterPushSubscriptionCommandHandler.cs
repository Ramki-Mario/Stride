using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application.Repositories;

namespace STRIDE.Modules.Notifications.Application.Commands.Push;

internal sealed class UnregisterPushSubscriptionCommandHandler
    : IRequestHandler<UnregisterPushSubscriptionCommand, Result>
{
    private readonly IPushSubscriptionRepository _pushSubs;

    public UnregisterPushSubscriptionCommandHandler(IPushSubscriptionRepository pushSubs)
    {
        _pushSubs = pushSubs;
    }

    public async Task<Result> Handle(UnregisterPushSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _pushSubs.GetByEndpointAsync(request.TenantId, request.Endpoint, cancellationToken);
        if (existing is null)
            return Result.Success();   // already gone — idempotent

        await _pushSubs.RemoveAsync(existing, cancellationToken);
        await _pushSubs.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
