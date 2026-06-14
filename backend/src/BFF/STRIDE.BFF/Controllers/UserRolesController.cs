using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for user-level role assignment on the Identity surface.
///
///   GET    /bff/identity/users/{userId}/roles              — list current custom roles
///   POST   /bff/identity/users/{userId}/roles              — assign a custom role
///   DELETE /bff/identity/users/{userId}/roles/{roleId}     — revoke a custom role
/// </summary>
[ApiController]
[Authorize]
[Route("bff/identity/users")]
public sealed class UserRolesController : ControllerBase
{
    private readonly IdentityApiClient           _identity;
    private readonly ILogger<UserRolesController> _logger;

    public UserRolesController(IdentityApiClient identity, ILogger<UserRolesController> logger)
    {
        _identity = identity;
        _logger   = logger;
    }

    [HttpGet("{userId:guid}/roles")]
    public async Task<IActionResult> GetUserRoles(Guid userId, CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _identity.GetUserRolesAsync(userId, token, cancellationToken), cancellationToken);
    }

    [HttpPost("{userId:guid}/roles")]
    public async Task<IActionResult> AssignUserRole(
        Guid userId, [FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _identity.AssignUserRoleAsync(userId, body, token, cancellationToken), cancellationToken);
    }

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RevokeUserRole(
        Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        var token = await HttpContext.GetCurrentAccessTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _identity.RevokeUserRoleAsync(userId, roleId, token, cancellationToken), cancellationToken);
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
                "Host identity/users roles API returned {StatusCode}: {Body}",
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
