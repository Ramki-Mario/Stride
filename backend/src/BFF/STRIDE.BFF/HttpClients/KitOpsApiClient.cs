using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the KitOps module surface of STRIDE.Host.
///
/// BFF route → Host route mapping:
///   GET  /bff/kit-ops/kit-items                     → GET  /api/kit-items
///   POST /bff/kit-ops/kit-items                     → POST /api/kit-items
///   GET  /bff/kit-ops/kit-items/{id}                → GET  /api/kit-items/{id}
///   GET  /bff/kit-ops/kit-items/{id}/availability   → GET  /api/kit-items/{id}/availability
///   PUT  /bff/kit-ops/kit-items/{id}                → PUT  /api/kit-items/{id}
///   PUT  /bff/kit-ops/kit-items/{id}/deactivate     → PUT  /api/kit-items/{id}/deactivate
///   PUT  /bff/kit-ops/kit-items/{id}/reactivate     → PUT  /api/kit-items/{id}/reactivate
/// </summary>
public sealed class KitOpsApiClient
{
    private readonly HttpClient _client;

    public KitOpsApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetKitItemsAsync(
        string token, int page, int pageSize, string? search, string? category, bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(search))   parts.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrEmpty(category)) parts.Add($"category={Uri.EscapeDataString(category)}");
        if (isActive.HasValue)               parts.Add($"isActive={isActive.Value.ToString().ToLower()}");
        var qs = "?" + string.Join("&", parts);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/kit-items{qs}", token), cancellationToken);
    }

    public Task<HttpResponseMessage> GetKitItemByIdAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/kit-items/{id}", token), cancellationToken);

    public Task<HttpResponseMessage> GetKitItemAvailabilityAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/kit-items/{id}/availability", token), cancellationToken);

    public Task<HttpResponseMessage> CreateKitItemAsync(
        object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/kit-items", body, token), cancellationToken);

    public Task<HttpResponseMessage> UpdateKitItemAsync(
        Guid id, object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/kit-items/{id}", body, token), cancellationToken);

    public Task<HttpResponseMessage> DeactivateKitItemAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/kit-items/{id}/deactivate", null, token), cancellationToken);

    public Task<HttpResponseMessage> ReactivateKitItemAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/kit-items/{id}/reactivate", null, token), cancellationToken);

    // ── helpers ───────────────────────────────────────────────────────────────

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
