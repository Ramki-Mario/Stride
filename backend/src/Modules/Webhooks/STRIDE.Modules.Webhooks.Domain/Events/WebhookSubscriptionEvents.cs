using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Webhooks.Domain.Events;

public sealed record WebhookSubscriptionCreatedEvent(Guid SubscriptionId, Guid TenantId, string Url, Guid CreatedBy) : IDomainEvent;
public sealed record WebhookSubscriptionUpdatedEvent(Guid SubscriptionId, Guid TenantId, Guid UpdatedBy) : IDomainEvent;
public sealed record WebhookSubscriptionDeletedEvent(Guid SubscriptionId, Guid TenantId, Guid DeletedBy) : IDomainEvent;
public sealed record WebhookSecretRegeneratedEvent(Guid SubscriptionId, Guid TenantId, Guid RegeneratedBy) : IDomainEvent;
