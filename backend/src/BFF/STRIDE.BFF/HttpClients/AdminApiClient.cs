using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Administration module user-management surface of STRIDE.Host.
///
/// BFF route → Host route mapping:
///   GET    /bff/administration/users              → GET    /api/administration/users
///   POST   /bff/administration/users/invite       → POST   /api/administration/users/invite
///   PUT    /bff/administration/users/{id}/role    → PUT    /api/administration/users/{id}/role
///   PUT    /bff/administration/users/{id}/deactivate  → PUT /api/administration/users/{id}/deactivate
///   PUT    /bff/administration/users/{id}/reactivate  → PUT /api/administration/users/{id}/reactivate
///   GET    /bff/administration/audit-log          → GET    /api/administration/audit-log
/// </summary>
public sealed class AdminApiClient
{
    private readonly HttpClient _client;

    public AdminApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetUsersAsync(
        string token, int page, int pageSize,
        string? search, string? role, string? status,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildQs(page, pageSize, search, role, status);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/administration/users{qs}", token), cancellationToken);
    }

    public Task<HttpResponseMessage> InviteUserAsync(object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/administration/users/invite", body, token), cancellationToken);

    public Task<HttpResponseMessage> UpdateUserRoleAsync(Guid userId, object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/administration/users/{userId}/role", body, token), cancellationToken);

    public Task<HttpResponseMessage> DeactivateUserAsync(Guid userId, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/administration/users/{userId}/deactivate", null, token), cancellationToken);

    public Task<HttpResponseMessage> ReactivateUserAsync(Guid userId, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/administration/users/{userId}/reactivate", null, token), cancellationToken);

    public Task<HttpResponseMessage> GetAuditLogAsync(
        string token,
        int page, int pageSize,
        string? from, string? to, string? action,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildAuditQs(page, pageSize, from, to, action);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/administration/audit-log{qs}", token), cancellationToken);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string BuildAuditQs(int page, int pageSize, string? from, string? to, string? action)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(from))   parts.Add($"from={Uri.EscapeDataString(from)}");
        if (!string.IsNullOrEmpty(to))     parts.Add($"to={Uri.EscapeDataString(to)}");
        if (!string.IsNullOrEmpty(action)) parts.Add($"action={Uri.EscapeDataString(action)}");
        return "?" + string.Join("&", parts);
    }

    private static string BuildQs(int page, int pageSize, string? search, string? role, string? status)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(search)) parts.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrEmpty(role))   parts.Add($"role={Uri.EscapeDataString(role)}");
        if (!string.IsNullOrEmpty(status)) parts.Add($"status={Uri.EscapeDataString(status)}");
        return "?" + string.Join("&", parts);
    }

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
