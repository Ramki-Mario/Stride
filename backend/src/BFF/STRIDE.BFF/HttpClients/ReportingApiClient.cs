using System.Net.Http.Headers;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Reporting module surface of STRIDE.Host (ADR-011).
///
/// The BFF extracts the stored JWT from the session (<c>access_token</c>) and
/// forwards it as a Bearer token so the Host can authenticate and apply tenant
/// isolation on every reporting query.
///
/// Routes proxied:
///   GET /api/reporting/dashboard/kpis
///   GET /api/reporting/dashboard/trends?days=N
///   GET /api/reporting/dashboard/alerts
///   GET /api/reporting/reports
///   POST /api/reporting/reports/generate
///   GET /api/reporting/reports/{id}/export
/// </summary>
public sealed class ReportingApiClient
{
    private readonly HttpClient _client;

    public ReportingApiClient(HttpClient client) => _client = client;

    // ── Dashboard ─────────────────────────────────────────────────────────

    /// <summary>Proxies GET /api/reporting/dashboard/kpis.</summary>
    public Task<HttpResponseMessage> GetDashboardKpisAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(HttpMethod.Get, "/api/reporting/dashboard/kpis", accessToken);
        return _client.SendAsync(request, cancellationToken);
    }

    /// <summary>Proxies GET /api/reporting/dashboard/alerts.</summary>
    public Task<HttpResponseMessage> GetDashboardAlertsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(HttpMethod.Get, "/api/reporting/dashboard/alerts", accessToken);
        return _client.SendAsync(request, cancellationToken);
    }

    /// <summary>Proxies GET /api/reporting/dashboard/team-workload.</summary>
    public Task<HttpResponseMessage> GetDashboardTeamWorkloadAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(HttpMethod.Get, "/api/reporting/dashboard/team-workload", accessToken);
        return _client.SendAsync(request, cancellationToken);
    }

    /// <summary>Proxies GET /api/reporting/dashboard/trends?days=N.</summary>
    public Task<HttpResponseMessage> GetWorkflowTrendsAsync(
        int days,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(
            HttpMethod.Get,
            $"/api/reporting/dashboard/trends?days={days}",
            accessToken);
        return _client.SendAsync(request, cancellationToken);
    }

    // ── Reports ───────────────────────────────────────────────────────────

    /// <summary>Proxies GET /api/reporting/reports.</summary>
    public Task<HttpResponseMessage> GetReportsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(HttpMethod.Get, "/api/reporting/reports", accessToken);
        return _client.SendAsync(request, cancellationToken);
    }

    /// <summary>Proxies POST /api/reporting/reports/generate.</summary>
    public Task<HttpResponseMessage> GenerateReportAsync(
        HttpContent body,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(HttpMethod.Post, "/api/reporting/reports/generate", accessToken);
        request.Content = body;
        return _client.SendAsync(request, cancellationToken);
    }

    /// <summary>Proxies GET /api/reporting/reports/{id}/export — streams CSV.</summary>
    public Task<HttpResponseMessage> ExportReportCsvAsync(
        Guid reportId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(
            HttpMethod.Get,
            $"/api/reporting/reports/{reportId}/export",
            accessToken);
        return _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static HttpRequestMessage BuildRequest(
        HttpMethod method,
        string relativeUri,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
