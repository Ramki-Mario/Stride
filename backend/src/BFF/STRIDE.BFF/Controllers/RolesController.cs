using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for the Identity roles catalogue.
///
///   GET  /bff/identity/roles  — list all active roles for the current tenant
/// </summary>
[ApiController]
[Authorize]
[Route("bff/identity")]
public sealed class RolesController : ControllerBase
{
    private readonly IdentityApiClient          _identity;
    private readonly ILogger<RolesController>   _logger;

    public RolesController(IdentityApiClient identity, ILogger<RolesController> logger)
    {
        _identity = identity;
        _logger   = logger;
    }

    /// <summary>Returns all active roles ordered by name (for workflow-builder dropdown).</summary>
    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();

        var response = await _identity.ListRolesAsync(token, cancellationToken);
        return await ProxyAsync(response, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<IActionResult> ProxyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host identity/roles API returned {StatusCode}: {Body}",
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
