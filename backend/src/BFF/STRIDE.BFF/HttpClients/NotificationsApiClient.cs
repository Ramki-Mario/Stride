using System.Net.Http.Headers;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Notifications module surface of STRIDE.Host (ADR-011).
///
/// Extracts the JWT from the Redis session and forwards it as a Bearer token so
/// the Host can authenticate and apply tenant isolation on every notification operation.
///
/// BFF route → Host route mapping:
///   GET    /bff/notifications              → GET    /api/notifications
///   GET    /bff/notifications/unread-count → GET    /api/notifications/unread-count
///   POST   /bff/notifications/{id}/read   → POST   /api/notifications/{id}/read
///   DELETE /bff/notifications/{id}        → DELETE /api/notifications/{id}
/// </summary>
public sealed class NotificationsApiClient
{
    private readonly HttpClient _client;

    public NotificationsApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetNotificationsAsync(string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/notifications", token), cancellationToken);

    public Task<HttpResponseMessage> GetUnreadCountAsync(string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/notifications/unread-count", token), cancellationToken);

    public Task<HttpResponseMessage> MarkAsReadAsync(Guid id, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/notifications/{id}/read", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteNotificationAsync(Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Delete, $"/api/notifications/{id}", token), cancellationToken);

    private static HttpRequestMessage Build(HttpMethod method, string uri, string token)
    {
        var req = new HttpRequestMessage(method, uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
