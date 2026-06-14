using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for the Identity roles and permissions surface.
///
///   GET    /bff/identity/permissions         — full permission catalog
///   GET    /bff/identity/roles               — list all active roles
///   GET    /bff/identity/roles/{id}          — single role with permissions
///   POST   /bff/identity/roles               — create custom role
///   PUT    /bff/identity/roles/{id}          — update role name/desc/permissions
///   DELETE /bff/identity/roles/{id}          — soft-delete custom role
/// </summary>
[ApiController]
[Authorize]
[Route("bff/identity")]
public sealed class RolesController : ControllerBase
{
    private readonly IdentityApiClient        _identity;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IdentityApiClient identity, ILogger<RolesController> logger)
    {
        _identity = identity;
        _logger   = logger;
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> ListPermissions(CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _identity.ListPermissionsAsync(token, cancellationToken), cancellationToken);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _identity.ListRolesAsync(token, cancellationToken), cancellationToken);
    }

    [HttpGet("roles/{id:guid}")]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _identity.GetRoleAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(
        [FromBody] object body,
        CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _identity.CreateRoleAsync(body, token, cancellationToken), cancellationToken);
    }

    [HttpPut("roles/{id:guid}")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] object body,
        CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _identity.UpdateRoleAsync(id, body, token, cancellationToken), cancellationToken);
    }

    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _identity.DeleteRoleAsync(id, token, cancellationToken), cancellationToken);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ProxyAsync(
        HttpResponseMessage response,
        CancellationToken   cancellationToken = default)
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

        if (string.IsNullOrWhiteSpace(json))
            return StatusCode((int)response.StatusCode);

        return new ContentResult
        {
            Content     = json,
            ContentType = "application/json",
            StatusCode  = (int)response.StatusCode,
        };
    }
}
