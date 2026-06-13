using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Queries.GetWebhookSubscriptions;

internal sealed class GetWebhookSubscriptionsQueryHandler
    : IRequestHandler<GetWebhookSubscriptionsQuery, Result<IReadOnlyList<WebhookSubscriptionDto>>>
{
    private readonly IWebhookReadService _reads;

    public GetWebhookSubscriptionsQueryHandler(IWebhookReadService reads) => _reads = reads;

    public async Task<Result<IReadOnlyList<WebhookSubscriptionDto>>> Handle(
        GetWebhookSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subs = await _reads.GetSubscriptionsAsync(request.TenantId, cancellationToken);
        return Result<IReadOnlyList<WebhookSubscriptionDto>>.Success(subs);
    }
}
