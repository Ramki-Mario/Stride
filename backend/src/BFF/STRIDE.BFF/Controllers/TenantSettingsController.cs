using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for tenant settings endpoints.
///
///   GET  /bff/administration/settings  — get current tenant settings
///   PUT  /bff/administration/settings  — update tenant settings
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
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _settings.GetSettingsAsync(token, ct));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] object body, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _settings.UpdateSettingsAsync(body, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetTokenAsync("access_token");

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

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
