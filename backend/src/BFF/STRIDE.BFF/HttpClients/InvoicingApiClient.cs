using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Invoicing module surface of STRIDE.Host.
///
/// BFF route → Host route mapping:
///   GET    /bff/invoicing/invoices              → GET    /api/invoicing/invoices
///   POST   /bff/invoicing/invoices              → POST   /api/invoicing/invoices
///   GET    /bff/invoicing/invoices/{id}         → GET    /api/invoicing/invoices/{id}
///   PUT    /bff/invoicing/invoices/{id}/send    → PUT    /api/invoicing/invoices/{id}/send
///   PUT    /bff/invoicing/invoices/{id}/paid    → PUT    /api/invoicing/invoices/{id}/paid
///   PUT    /bff/invoicing/invoices/{id}/void    → PUT    /api/invoicing/invoices/{id}/void
/// </summary>
public sealed class InvoicingApiClient
{
    private readonly HttpClient _client;

    public InvoicingApiClient(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> GetInvoicesAsync(
        string token, int page, int pageSize, string? search, int? status,
        CancellationToken ct = default)
    {
        var qs = BuildQs(page, pageSize, search, status);
        return _client.SendAsync(Build(HttpMethod.Get, $"/api/invoicing/invoices{qs}", token), ct);
    }

    public Task<HttpResponseMessage> GetInvoiceByIdAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/invoicing/invoices/{id}", token), ct);

    public Task<HttpResponseMessage> GenerateInvoiceAsync(object body, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Post, "/api/invoicing/invoices", body, token), ct);

    public Task<HttpResponseMessage> SendInvoiceAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/invoicing/invoices/{id}/send", null, token), ct);

    public Task<HttpResponseMessage> MarkPaidAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/invoicing/invoices/{id}/paid", null, token), ct);

    public Task<HttpResponseMessage> VoidInvoiceAsync(Guid id, string token, CancellationToken ct = default)
        => _client.SendAsync(BuildWithBody(HttpMethod.Put, $"/api/invoicing/invoices/{id}/void", null, token), ct);

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string BuildQs(int page, int pageSize, string? search, int? status)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(search)) parts.Add($"search={Uri.EscapeDataString(search)}");
        if (status.HasValue) parts.Add($"status={status.Value}");
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
