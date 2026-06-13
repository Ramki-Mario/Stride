using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Queries.GetWebhookDeliveries;

public sealed record GetWebhookDeliveriesQuery(
    Guid TenantId,
    Guid SubscriptionId,
    int  Limit = 50)
    : IRequest<Result<IReadOnlyList<WebhookDeliveryDto>>>;
