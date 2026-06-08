using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Teams module surface of STRIDE.Host.
///
/// BFF route → Host route mapping:
///   GET  /bff/teams              → GET  /api/teams
///   POST /bff/teams              → POST /api/teams
///   GET  /bff/teams/{id}         → GET  /api/teams/{id}
///   PUT  /bff/teams/{id}         → PUT  /api/teams/{id}
///   PUT  /bff/teams/{id}/deactivate  → PUT /api/teams/{id}/deactivate
///   PUT  /bff/teams/{id}/reactivate  → PUT /api/teams/{id}/reactivate
/// </summary>
public sealed class TeamsApiClient
{
    private readonly HttpClient _client;

    public TeamsApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetTeamsAsync(
        string token, int page, int pageSize, string? search, int? status,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(search)) parts.Add($"search={Uri.EscapeDataString(search)}");
        if (status.HasValue) parts.Add($"status={status.Value}");
        var qs = "?" + string.Join("&", parts);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/teams{qs}", token), cancellationToken);
    }

    public Task<HttpResponseMessage> GetTeamByIdAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/teams/{id}", token), cancellationToken);

    public Task<HttpResponseMessage> CreateTeamAsync(
        object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/teams", body, token), cancellationToken);

    public Task<HttpResponseMessage> UpdateTeamAsync(
        Guid id, object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/teams/{id}", body, token), cancellationToken);

    public Task<HttpResponseMessage> DeactivateTeamAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/teams/{id}/deactivate", null, token), cancellationToken);

    public Task<HttpResponseMessage> ReactivateTeamAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/teams/{id}/reactivate", null, token), cancellationToken);

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
