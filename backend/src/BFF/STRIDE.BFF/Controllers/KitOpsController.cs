using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for KitOps kit-items endpoints (Admin catalog management).
///
///   GET  /bff/kit-ops/kit-items                   — paged kit item list
///   POST /bff/kit-ops/kit-items                   — create kit item (Admin)
///   GET  /bff/kit-ops/kit-items/{id}              — kit item detail
///   GET  /bff/kit-ops/kit-items/{id}/availability — live availability
///   PUT  /bff/kit-ops/kit-items/{id}              — update kit item (Admin)
///   PUT  /bff/kit-ops/kit-items/{id}/deactivate   — deactivate (Admin)
///   PUT  /bff/kit-ops/kit-items/{id}/reactivate   — reactivate (Admin)
/// </summary>
[ApiController]
[Authorize]
[Route("bff/kit-ops")]
public sealed class KitOpsController : ControllerBase
{
    private readonly KitOpsApiClient           _kitOps;
    private readonly ILogger<KitOpsController> _logger;

    public KitOpsController(KitOpsApiClient kitOps, ILogger<KitOpsController> logger)
    {
        _kitOps = kitOps;
        _logger = logger;
    }

    [HttpGet("kit-items")]
    public async Task<IActionResult> GetKitItems(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] string? category = null,
        [FromQuery] bool?   isActive = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _kitOps.GetKitItemsAsync(token, page, pageSize, search, category, isActive, cancellationToken),
            cancellationToken);
    }

    [HttpPost("kit-items")]
    public async Task<IActionResult> CreateKitItem([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.CreateKitItemAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpGet("kit-items/{id:guid}")]
    public async Task<IActionResult> GetKitItem(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _kitOps.GetKitItemByIdAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpGet("kit-items/{id:guid}/availability")]
    public async Task<IActionResult> GetKitItemAvailability(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _kitOps.GetKitItemAvailabilityAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpPut("kit-items/{id:guid}")]
    public async Task<IActionResult> UpdateKitItem(
        Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.UpdateKitItemAsync(id, body, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("kit-items/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateKitItem(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.DeactivateKitItemAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("kit-items/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateKitItem(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.ReactivateKitItemAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetCurrentAccessTokenAsync();

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host kit-ops API returned {StatusCode}: {Body}",
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
