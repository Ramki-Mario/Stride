using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Webhooks.Application.Commands.DeleteWebhookSubscription;

public sealed record DeleteWebhookSubscriptionCommand(
    Guid TenantId,
    Guid SubscriptionId,
    Guid DeletedBy) : IRequest<Result>;
