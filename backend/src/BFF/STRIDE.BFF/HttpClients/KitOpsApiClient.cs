using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the KitOps module surface of STRIDE.Host.
///
/// BFF route → Host route mapping:
///   GET  /bff/kit-ops/catalog                       → GET  /api/kit-items/catalog
///   GET  /bff/kit-ops/kit-items                     → GET  /api/kit-items
///   POST /bff/kit-ops/kit-items                     → POST /api/kit-items
///   GET  /bff/kit-ops/kit-items/{id}                → GET  /api/kit-items/{id}
///   GET  /bff/kit-ops/kit-items/{id}/availability   → GET  /api/kit-items/{id}/availability
///   PUT  /bff/kit-ops/kit-items/{id}                → PUT  /api/kit-items/{id}
///   PUT  /bff/kit-ops/kit-items/{id}/deactivate     → PUT  /api/kit-items/{id}/deactivate
///   PUT  /bff/kit-ops/kit-items/{id}/reactivate     → PUT  /api/kit-items/{id}/reactivate
///   GET  /bff/kit-ops/checkouts/my                  → GET  /api/kit-checkouts/my
///   POST /bff/kit-ops/checkouts                     → POST /api/kit-checkouts
///   PUT  /bff/kit-ops/checkouts/{id}/return         → PUT  /api/kit-checkouts/{id}/return
///   GET  /bff/kit-ops/reservations/my               → GET  /api/kit-reservations/my
///   POST /bff/kit-ops/reservations                  → POST /api/kit-reservations
///   PUT  /bff/kit-ops/reservations/{id}/cancel      → PUT  /api/kit-reservations/{id}/cancel
/// </summary>
public sealed class KitOpsApiClient
{
    private readonly HttpClient _client;

    public KitOpsApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetCatalogAsync(
        string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/kit-items/catalog", token), cancellationToken);

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

    // ── Checkout / return ────────────────────────────────────────────────────

    public Task<HttpResponseMessage> GetMyCheckoutsAsync(
        string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/kit-checkouts/my", token), cancellationToken);

    public Task<HttpResponseMessage> CheckoutAsync(
        object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/kit-checkouts", body, token), cancellationToken);

    public Task<HttpResponseMessage> ReturnAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/kit-checkouts/{id}/return", null, token), cancellationToken);

    // ── Reservations ─────────────────────────────────────────────────────────

    public Task<HttpResponseMessage> GetMyReservationsAsync(
        string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/kit-reservations/my", token), cancellationToken);

    public Task<HttpResponseMessage> CreateReservationAsync(
        object body, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/kit-reservations", body, token), cancellationToken);

    public Task<HttpResponseMessage> CancelReservationAsync(
        Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/kit-reservations/{id}/cancel", null, token), cancellationToken);

    // ── Reports ───────────────────────────────────────────────────────────────

    public Task<HttpResponseMessage> GetCheckoutHistoryAsync(
        string token, DateTime? from, DateTime? to, Guid? kitItemId,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildReportQs(from, to, kitItemId);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/kit-reports/checkout-history{qs}", token), cancellationToken);
    }

    public Task<HttpResponseMessage> ExportCheckoutHistoryAsync(
        string token, DateTime? from, DateTime? to, Guid? kitItemId,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildReportQs(from, to, kitItemId);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/kit-reports/checkout-history/export{qs}", token), cancellationToken);
    }

    public Task<HttpResponseMessage> GetUsageSummaryAsync(
        string token, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildReportQs(from, to, null);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/kit-reports/usage-summary{qs}", token), cancellationToken);
    }

    public Task<HttpResponseMessage> ExportUsageSummaryAsync(
        string token, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var qs = BuildReportQs(from, to, null);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/kit-reports/usage-summary/export{qs}", token), cancellationToken);
    }

    private static string BuildReportQs(DateTime? from, DateTime? to, Guid? kitItemId)
    {
        var parts = new List<string>();
        if (from.HasValue)       parts.Add($"from={from.Value:O}");
        if (to.HasValue)         parts.Add($"to={to.Value:O}");
        if (kitItemId.HasValue)  parts.Add($"kitItemId={kitItemId.Value}");
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

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
