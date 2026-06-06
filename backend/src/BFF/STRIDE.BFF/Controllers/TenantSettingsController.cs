using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for tenant settings endpoints.
///
///   GET  /bff/administration/settings              — get current tenant settings
///   PUT  /bff/administration/settings              — update settings (+ CSS sanitisation report)
///   GET  /bff/administration/settings/css-template — download --stride-* token template file
/// </summary>
[ApiController]
[Authorize]
[Route("bff/administration/settings")]
public sealed class TenantSettingsController : ControllerBase
{
    private readonly TenantSettingsApiClient           _settings;
    private readonly ILogger<TenantSettingsController> _logger;

    public TenantSettingsController(
        TenantSettingsApiClient settings, ILogger<TenantSettingsController> logger)
    {
        _settings = settings;
        _logger   = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _settings.GetSettingsAsync(token, cancellationToken), cancellationToken);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _settings.UpdateSettingsAsync(body, token, cancellationToken);

        if (!response.IsSuccessStatusCode) return await ProxyAsync(response, cancellationToken);

        // 200 means CSS was included and a sanitisation report was returned; proxy it.
        // 204 means settings-only update with no CSS.
        return response.StatusCode == System.Net.HttpStatusCode.NoContent
            ? NoContent()
            : await ProxyAsync(response, cancellationToken);
    }

    /// <summary>GET /bff/administration/settings/css-template — proxies the CSS file download.</summary>
    [HttpGet("css-template")]
    public async Task<IActionResult> GetCssTemplate(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _settings.GetCssTemplateAsync(token, cancellationToken);
        if (!response.IsSuccessStatusCode) return await ProxyAsync(response, cancellationToken);

        var css = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return File(css, "text/css", "stride-theme-template.css");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetTokenAsync("access_token");

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host administration/settings API returned {StatusCode}: {Body}",
                (int)response.StatusCode, json);

            return StatusCode((int)response.StatusCode,
                new ProblemDetails
                {
                    Title  = "Upstream error",
                    Detail = json,
                    Status = (int)response.StatusCode,
                });
        }

        return new ContentResult
        {
            Content     = json,
            ContentType = "application/json",
            StatusCode  = StatusCodes.Status200OK,
        };
    }
}
