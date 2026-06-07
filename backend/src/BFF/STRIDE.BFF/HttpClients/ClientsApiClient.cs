using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Clients module surface of STRIDE.Host.
///
/// BFF route → Host route mapping:
///   GET  /bff/clients              → GET  /api/clients
///   POST /bff/clients              → POST /api/clients
///   GET  /bff/clients/{id}         → GET  /api/clients/{id}
///   PUT  /bff/clients/{id}         → PUT  /api/clients/{id}
///   PUT  /bff/clients/{id}/deactivate → PUT /api/clients/{id}/deactivate
/// </summary>
public sealed class ClientsApiClient
{
    private readonly HttpClient _client;

    public ClientsApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetClientsAsync(
        string token, int page, int pageSize, string? search, int? status,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(search)) parts.Add($"search={Uri.EscapeDataString(search)}");
        if (status.HasValue) parts.Add($"status={status.Value}");
        var qs = "?" + string.Join("&", parts);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/clients{qs}", token), cancellationToken);
    }

    public Task<HttpResponseMessage> GetClientByIdAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/clients/{id}", token), cancellationToken);

    public Task<HttpResponseMessage> CreateClientAsync(
        object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/clients", body, token), cancellationToken);

    public Task<HttpResponseMessage> UpdateClientAsync(
        Guid id, object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/clients/{id}", body, token), cancellationToken);

    public Task<HttpResponseMessage> DeactivateClientAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/clients/{id}/deactivate", null, token), cancellationToken);

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
