using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Commands.CreateWebhookSubscription;

public sealed record CreateWebhookSubscriptionCommand(
    Guid                  TenantId,
    string                Url,
    IReadOnlyList<string> EventTypes,
    Guid                  CreatedBy) : IRequest<Result<WebhookSecretDto>>;
