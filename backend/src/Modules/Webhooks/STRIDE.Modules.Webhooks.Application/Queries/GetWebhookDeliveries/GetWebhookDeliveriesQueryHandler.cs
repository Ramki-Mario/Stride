using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Queries.GetWebhookDeliveries;

internal sealed class GetWebhookDeliveriesQueryHandler
    : IRequestHandler<GetWebhookDeliveriesQuery, Result<IReadOnlyList<WebhookDeliveryDto>>>
{
    private readonly IWebhookDeliveryReadService _reads;

    public GetWebhookDeliveriesQueryHandler(IWebhookDeliveryReadService reads) => _reads = reads;

    public async Task<Result<IReadOnlyList<WebhookDeliveryDto>>> Handle(
        GetWebhookDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var deliveries = await _reads.GetBySubscriptionAsync(
            request.TenantId, request.SubscriptionId, request.Limit, cancellationToken);

        return Result<IReadOnlyList<WebhookDeliveryDto>>.Success(deliveries);
    }
}
