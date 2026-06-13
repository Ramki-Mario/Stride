using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Webhooks.Application.Commands.UpdateWebhookSubscription;

public sealed record UpdateWebhookSubscriptionCommand(
    Guid                  TenantId,
    Guid                  SubscriptionId,
    string                Url,
    IReadOnlyList<string> EventTypes,
    bool                  IsActive,
    Guid                  UpdatedBy) : IRequest<Result>;
