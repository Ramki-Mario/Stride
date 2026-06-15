using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for all KitOps endpoints.
///
/// Field user (any authenticated):
///   GET  /bff/kit-ops/catalog                     — all active items with live availability
///   GET  /bff/kit-ops/checkouts/my                — caller's checkout history
///   POST /bff/kit-ops/checkouts                   — check out a kit item
///   PUT  /bff/kit-ops/checkouts/{id}/return       — return a kit item
///   GET  /bff/kit-ops/reservations/my             — caller's reservation history
///   POST /bff/kit-ops/reservations                — request a kit item (reservation)
///   PUT  /bff/kit-ops/reservations/{id}/cancel    — cancel your pending reservation
///
/// Admin only:
///   GET  /bff/kit-ops/kit-items                   — paged kit item list
///   POST /bff/kit-ops/kit-items                   — create kit item
///   GET  /bff/kit-ops/kit-items/{id}              — kit item detail
///   GET  /bff/kit-ops/kit-items/{id}/availability — live availability snapshot
///   PUT  /bff/kit-ops/kit-items/{id}              — update kit item
///   PUT  /bff/kit-ops/kit-items/{id}/deactivate   — deactivate
///   PUT  /bff/kit-ops/kit-items/{id}/reactivate   — reactivate
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

    // ── Field user endpoints ─────────────────────────────────────────────────

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _kitOps.GetCatalogAsync(token, cancellationToken), cancellationToken);
    }

    [HttpGet("checkouts/my")]
    public async Task<IActionResult> GetMyCheckouts(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _kitOps.GetMyCheckoutsAsync(token, cancellationToken), cancellationToken);
    }

    [HttpPost("checkouts")]
    public async Task<IActionResult> Checkout([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.CheckoutAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("checkouts/{id:guid}/return")]
    public async Task<IActionResult> Return(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.ReturnAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpGet("reservations/my")]
    public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _kitOps.GetMyReservationsAsync(token, cancellationToken), cancellationToken);
    }

    [HttpPost("reservations")]
    public async Task<IActionResult> CreateReservation([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.CreateReservationAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("reservations/{id:guid}/cancel")]
    public async Task<IActionResult> CancelReservation(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _kitOps.CancelReservationAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // ── Admin kit-items endpoints ────────────────────────────────────────────

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

    // ── Report endpoints (Admin only) ─────────────────────────────────────────

    [HttpGet("reports/checkout-history")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCheckoutHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid?     kitItemId,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _kitOps.GetCheckoutHistoryAsync(token, from, to, kitItemId, cancellationToken),
            cancellationToken);
    }

    [HttpGet("reports/checkout-history/export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportCheckoutHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid?     kitItemId,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyFileAsync(
            await _kitOps.ExportCheckoutHistoryAsync(token, from, to, kitItemId, cancellationToken),
            cancellationToken);
    }

    [HttpGet("reports/usage-summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsageSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _kitOps.GetUsageSummaryAsync(token, from, to, cancellationToken),
            cancellationToken);
    }

    [HttpGet("reports/usage-summary/export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportUsageSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyFileAsync(
            await _kitOps.ExportUsageSummaryAsync(token, from, to, cancellationToken),
            cancellationToken);
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

    private async Task<IActionResult> ProxyFileAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Host kit-ops report export returned {StatusCode}: {Body}",
                (int)response.StatusCode, body);
            return StatusCode((int)response.StatusCode,
                new ProblemDetails { Title = "Upstream error", Detail = body, Status = (int)response.StatusCode });
        }

        var bytes       = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString()
                          ?? "application/octet-stream";
        var disposition = response.Content.Headers.ContentDisposition?.ToString();

        if (!string.IsNullOrEmpty(disposition))
            Response.Headers["Content-Disposition"] = disposition;

        return File(bytes, contentType);
    }
}
