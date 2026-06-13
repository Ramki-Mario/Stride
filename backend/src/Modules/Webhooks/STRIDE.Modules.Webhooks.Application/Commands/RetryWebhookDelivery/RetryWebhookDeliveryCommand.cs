using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Webhooks.Application.Commands.RetryWebhookDelivery;

public sealed record RetryWebhookDeliveryCommand(
    Guid TenantId,
    Guid SubscriptionId,
    Guid DeliveryId) : IRequest<Result>;
