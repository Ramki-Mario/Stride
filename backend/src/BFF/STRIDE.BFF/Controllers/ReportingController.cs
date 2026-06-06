using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Reporting module endpoints.
///
/// Forwards authenticated calls from the Angular SPA to STRIDE.Host, injecting
/// the session-stored JWT so the Host can enforce tenant isolation.
///
/// Routes:
///   GET  /bff/reporting/dashboard/kpis             → GET  /api/reporting/dashboard/kpis
///   GET  /bff/reporting/dashboard/trends?days=N    → GET  /api/reporting/dashboard/trends?days=N
///   GET  /bff/reporting/reports                    → GET  /api/reporting/reports
///   POST /bff/reporting/reports/generate            → POST /api/reporting/reports/generate
///   GET  /bff/reporting/reports/{id}/export         → GET  /api/reporting/reports/{id}/export
/// </summary>
[ApiController]
[Authorize]
[Route("bff/reporting")]
public sealed class ReportingController : ControllerBase
{
    private readonly ReportingApiClient            _reporting;
    private readonly ILogger<ReportingController>  _logger;

    public ReportingController(
        ReportingApiClient reporting,
        ILogger<ReportingController> logger)
    {
        _reporting = reporting;
        _logger    = logger;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────

    /// <summary>GET /bff/reporting/dashboard/kpis</summary>
    [HttpGet("dashboard/kpis")]
    public async Task<IActionResult> GetDashboardKpis(CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _reporting.GetDashboardKpisAsync(token, cancellationToken);
        return await ProxyJsonResponseAsync(response, cancellationToken: cancellationToken);
    }

    /// <summary>GET /bff/reporting/dashboard/trends?days=N</summary>
    [HttpGet("dashboard/trends")]
    public async Task<IActionResult> GetWorkflowTrends(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _reporting.GetWorkflowTrendsAsync(days, token, cancellationToken);
        return await ProxyJsonResponseAsync(response, cancellationToken: cancellationToken);
    }

    // ── Reports ───────────────────────────────────────────────────────────

    /// <summary>GET /bff/reporting/reports</summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _reporting.GetReportsAsync(token, cancellationToken);
        return await ProxyJsonResponseAsync(response, cancellationToken: cancellationToken);
    }

    /// <summary>POST /bff/reporting/reports/generate</summary>
    [HttpPost("reports/generate")]
    public async Task<IActionResult> GenerateReport(CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync();
        if (token is null) return Unauthorized();

        // Forward the raw request body so the BFF doesn't need to know about the DTO.
        using var body = new StreamContent(Request.Body);
        body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = await _reporting.GenerateReportAsync(body, token, cancellationToken);
        return await ProxyJsonResponseAsync(response, forwardStatusCode: true, cancellationToken: cancellationToken);
    }

    /// <summary>GET /bff/reporting/reports/{id}/export — streams CSV to browser.</summary>
    [HttpGet("reports/{id:guid}/export")]
    public async Task<IActionResult> ExportReportCsv(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _reporting.ExportReportCsvAsync(id, token, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var content     = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/csv; charset=utf-8";
        var fileName    = response.Content.Headers.ContentDisposition?.FileName
                          ?? $"report-{id}.csv";

        return File(content, contentType, fileName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves the JWT stored in the Redis session.
    /// Returns null if the session has no access_token (should not happen for [Authorize] routes).
    /// </summary>
    private Task<string?> GetAccessTokenAsync() =>
        HttpContext.GetTokenAsync("access_token");

    /// <summary>
    /// Reads the Host response and returns it verbatim as JSON.
    /// Preserves the Host status code when <paramref name="forwardStatusCode"/> is true
    /// (e.g. 201 Created for POST /generate).
    /// </summary>
    private async Task<IActionResult> ProxyJsonResponseAsync(
        HttpResponseMessage response,
        bool forwardStatusCode = false,
        CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host reporting API returned {StatusCode}: {Body}",
                (int)response.StatusCode, json);

            return StatusCode((int)response.StatusCode,
                new ProblemDetails
                {
                    Title  = "Upstream error",
                    Detail = json,
                    Status = (int)response.StatusCode,
                });
        }

        var statusCode = forwardStatusCode ? (int)response.StatusCode : StatusCodes.Status200OK;
        return new ContentResult
        {
            Content     = json,
            ContentType = "application/json",
            StatusCode  = statusCode,
        };
    }
}
