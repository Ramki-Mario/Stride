using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Commands.RegenerateWebhookSecret;

public sealed record RegenerateWebhookSecretCommand(
    Guid TenantId,
    Guid SubscriptionId,
    Guid RegeneratedBy) : IRequest<Result<WebhookSecretDto>>;
