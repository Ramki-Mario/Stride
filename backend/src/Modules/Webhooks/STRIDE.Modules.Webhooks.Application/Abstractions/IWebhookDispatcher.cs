namespace STRIDE.Modules.Webhooks.Application.Abstractions;

/// <summary>
/// Dispatches a signed webhook payload to all active subscriptions for the given
/// tenant and event type. Delivery failures are logged but never re-thrown — the
/// caller (a domain event handler) must not fail because of a downstream endpoint.
/// </summary>
public interface IWebhookDispatcher
{
    /// <param name="tenantId">Scopes subscription lookup to the tenant that owns the event.</param>
    /// <param name="eventType">One of the keys defined in <c>WebhookEventTypes</c>.</param>
    /// <param name="data">Event-specific payload; serialised to JSON and included in the envelope.</param>
    Task DispatchAsync(Guid tenantId, string eventType, object data, CancellationToken cancellationToken = default);
}
