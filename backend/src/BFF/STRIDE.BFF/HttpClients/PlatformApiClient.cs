using System.Net.Http.Headers;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the platform-admin surface of STRIDE.Host.
/// All requests require a JWT with the "superadmin" claim — the BFF forwards
/// the session access token as a Bearer header.
///
/// BFF route → Host route mapping:
///   GET /bff/platform/tenants         → GET /api/platform/tenants
///   GET /bff/platform/tenants/{id}    → GET /api/platform/tenants/{id}
///   GET /bff/platform/stats           → GET /api/platform/stats
/// </summary>
public sealed class PlatformApiClient
{
    private readonly HttpClient _client;

    public PlatformApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetTenantsAsync(string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/platform/tenants", token), ct);

    public Task<HttpResponseMessage> GetTenantAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/platform/tenants/{id}", token), ct);

    public Task<HttpResponseMessage> GetStatsAsync(string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/platform/stats", token), ct);

    private static HttpRequestMessage Build(HttpMethod method, string uri, string token)
    {
        var req = new HttpRequestMessage(method, uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
