using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Webhooks.Domain.Events;

/// <summary>
/// Raised when all 4 delivery attempts for a webhook delivery have failed.
/// Triggers an admin notification so the tenant knows the integration is broken.
/// </summary>
public sealed record WebhookDeliveryExhaustedEvent(
    Guid DeliveryId,
    Guid TenantId,
    Guid SubscriptionId,
    string EventType) : IDomainEvent;
