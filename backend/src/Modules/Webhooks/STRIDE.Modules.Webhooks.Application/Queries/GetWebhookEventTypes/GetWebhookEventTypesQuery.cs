using MediatR;
using STRIDE.Modules.Webhooks.Application.DTOs;

namespace STRIDE.Modules.Webhooks.Application.Queries.GetWebhookEventTypes;

/// <summary>Returns the static catalog of subscribable event types (key + label) for the UI.</summary>
public sealed record GetWebhookEventTypesQuery : IRequest<IReadOnlyList<WebhookEventTypeDto>>;
