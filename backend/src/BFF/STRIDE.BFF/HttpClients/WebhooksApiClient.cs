using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Webhooks module surface of STRIDE.Host.
///
/// BFF route → Host route mapping:
///   GET    /bff/webhooks/event-types                                  → GET    /api/webhooks/event-types
///   GET    /bff/webhooks/subscriptions                                 → GET    /api/webhooks/subscriptions
///   POST   /bff/webhooks/subscriptions                                 → POST   /api/webhooks/subscriptions
///   PUT    /bff/webhooks/subscriptions/{id}                            → PUT    /api/webhooks/subscriptions/{id}
///   DELETE /bff/webhooks/subscriptions/{id}                            → DELETE /api/webhooks/subscriptions/{id}
///   POST   /bff/webhooks/subscriptions/{id}/test                       → POST   /api/webhooks/subscriptions/{id}/test
///   POST   /bff/webhooks/subscriptions/{id}/regenerate-secret          → POST   /api/webhooks/subscriptions/{id}/regenerate-secret
///   GET    /bff/webhooks/subscriptions/{id}/deliveries                 → GET    /api/webhooks/subscriptions/{id}/deliveries
///   POST   /bff/webhooks/subscriptions/{id}/deliveries/{dId}/retry     → POST   /api/webhooks/subscriptions/{id}/deliveries/{dId}/retry
/// </summary>
public sealed class WebhooksApiClient
{
    private readonly HttpClient _client;

    public WebhooksApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetEventTypesAsync(string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/webhooks/event-types", token), cancellationToken);

    public Task<HttpResponseMessage> GetSubscriptionsAsync(string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/webhooks/subscriptions", token), cancellationToken);

    public Task<HttpResponseMessage> CreateSubscriptionAsync(object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/webhooks/subscriptions", body, token), cancellationToken);

    public Task<HttpResponseMessage> UpdateSubscriptionAsync(Guid id, object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/webhooks/subscriptions/{id}", body, token), cancellationToken);

    public Task<HttpResponseMessage> DeleteSubscriptionAsync(Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Delete, $"/api/webhooks/subscriptions/{id}", token), cancellationToken);

    public Task<HttpResponseMessage> TestSubscriptionAsync(Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, $"/api/webhooks/subscriptions/{id}/test", null, token), cancellationToken);

    public Task<HttpResponseMessage> RegenerateSecretAsync(Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, $"/api/webhooks/subscriptions/{id}/regenerate-secret", null, token), cancellationToken);

    public Task<HttpResponseMessage> GetDeliveriesAsync(Guid id, int limit, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/webhooks/subscriptions/{id}/deliveries?limit={limit}", token), cancellationToken);

    public Task<HttpResponseMessage> RetryDeliveryAsync(Guid id, Guid deliveryId, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, $"/api/webhooks/subscriptions/{id}/deliveries/{deliveryId}/retry", null, token), cancellationToken);

    // ── helpers ──────────────────────────────────────────────────────────────

    private static HttpRequestMessage Build(HttpMethod method, string uri, string token)
    {
        var req = new HttpRequestMessage(method, uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage BuildWithBody(HttpMethod method, string uri, object? body, string token)
    {
        var req = Build(method, uri, token);
        req.Content = new StringContent(
            body is null ? "{}" : JsonSerializer.Serialize(body),
            Encoding.UTF8, "application/json");
        return req;
    }
}
