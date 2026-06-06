namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient that forwards health-check requests to STRIDE.Host (ADR-011).
///
/// Health endpoints are unauthenticated on the Host — no Bearer token required.
/// The BFF exposes /bff/health so the Angular SPA never hits the Host directly.
///
/// BFF route → Host route:
///   GET /bff/health → GET /health/ready  (verbose JSON via UIResponseWriter)
/// </summary>
public sealed class HealthApiClient
{
    private readonly HttpClient _client;

    public HealthApiClient(HttpClient client) => _client = client;

    /// <summary>Returns the raw verbose health JSON from the Host's /health/ready endpoint.</summary>
    public Task<HttpResponseMessage> GetHealthAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/health/ready", cancellationToken);
}
