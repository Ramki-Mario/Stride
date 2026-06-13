using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Queries.GetWebhookSubscriptions;

public sealed record GetWebhookSubscriptionsQuery(Guid TenantId)
    : IRequest<Result<IReadOnlyList<WebhookSubscriptionDto>>>;
