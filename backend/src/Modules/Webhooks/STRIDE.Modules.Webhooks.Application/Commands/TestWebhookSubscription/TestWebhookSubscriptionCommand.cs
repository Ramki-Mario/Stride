using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.Abstractions;

namespace STRIDE.Modules.Webhooks.Application.Commands.TestWebhookSubscription;

public sealed record TestWebhookSubscriptionCommand(
    Guid TenantId,
    Guid SubscriptionId) : IRequest<Result<WebhookTestResult>>;
