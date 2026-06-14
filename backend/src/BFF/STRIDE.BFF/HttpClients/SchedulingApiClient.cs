using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

public sealed class SchedulingApiClient
{
    private readonly HttpClient _client;

    public SchedulingApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> ListSchedulesAsync(string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/scheduling/schedules", token), ct);

    public Task<HttpResponseMessage> GetScheduleAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/scheduling/schedules/{id}", token), ct);

    public Task<HttpResponseMessage> CreateScheduleAsync(object body, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/scheduling/schedules", body, token), ct);

    public Task<HttpResponseMessage> UpdateScheduleAsync(Guid id, object body, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/scheduling/schedules/{id}", body, token), ct);

    public Task<HttpResponseMessage> DeleteScheduleAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Delete, $"/api/scheduling/schedules/{id}", token), ct);

    public Task<HttpResponseMessage> ActivateScheduleAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, $"/api/scheduling/schedules/{id}/activate", null, token), ct);

    public Task<HttpResponseMessage> DeactivateScheduleAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, $"/api/scheduling/schedules/{id}/deactivate", null, token), ct);

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
