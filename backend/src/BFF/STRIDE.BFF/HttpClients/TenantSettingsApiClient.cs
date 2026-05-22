using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Administration module tenant-settings surface.
///
///   GET  /bff/administration/settings              → GET  /api/administration/settings
///   PUT  /bff/administration/settings              → PUT  /api/administration/settings
///   GET  /bff/administration/settings/css-template → GET  /api/administration/settings/css-template
/// </summary>
public sealed class TenantSettingsApiClient
{
    private readonly HttpClient _client;

    public TenantSettingsApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetSettingsAsync(string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/administration/settings", token), ct);

    public Task<HttpResponseMessage> UpdateSettingsAsync(object body, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, "/api/administration/settings", body, token), ct);

    public Task<HttpResponseMessage> GetCssTemplateAsync(string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/administration/settings/css-template", token), ct);

    // ── helpers ───────────────────────────────────────────────────────────────

    private static HttpRequestMessage Build(HttpMethod method, string uri, string token)
    {
        var req = new HttpRequestMessage(method, uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage BuildWithBody(
        HttpMethod method, string uri, object? body, string token)
    {
        var req = Build(method, uri, token);
        if (body is not null)
            req.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return req;
    }
}
