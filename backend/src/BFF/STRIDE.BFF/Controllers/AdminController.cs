using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using STRIDE.BFF.Extensions;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Administration module user-management endpoints.
///
///   GET    /bff/administration/users              â€” paged user list
///   POST   /bff/administration/users/invite       â€” invite new user
///   PUT    /bff/administration/users/{id}/role    â€” change user role
///   PUT    /bff/administration/users/{id}/deactivate  â€” deactivate user
///   PUT    /bff/administration/users/{id}/reactivate  â€” reactivate user
///   GET    /bff/administration/audit-log          â€” paged audit log (Admin only)
/// </summary>
[ApiController]
[Authorize]
[Route("bff/administration")]
public sealed class AdminController : ControllerBase
{
    private readonly AdminApiClient            _admin;
    private readonly IdentityApiClient         _identity;
    private readonly ILogger<AdminController>  _logger;

    public AdminController(
        AdminApiClient           admin,
        IdentityApiClient        identity,
        ILogger<AdminController> logger)
    {
        _admin    = admin;
        _identity = identity;
        _logger   = logger;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] string? role     = null,
        [FromQuery] string? status   = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _admin.GetUsersAsync(token, page, pageSize, search, role, status, cancellationToken), cancellationToken);
    }

    [HttpPost("users/invite")]
    public async Task<IActionResult> InviteUser(
        [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();

        // Extract optional roleId before forwarding to the admin endpoint.
        Guid? roleId = null;
        if (body.TryGetProperty("roleId", out var roleIdEl) &&
            roleIdEl.ValueKind == JsonValueKind.String &&
            Guid.TryParse(roleIdEl.GetString(), out var rid))
        {
            roleId = rid;
        }

        var response = await _admin.InviteUserAsync(body, token, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return await ProxyAsync(response, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // If a custom roleId was supplied, assign it now that the user exists.
        if (roleId.HasValue)
        {
            using var doc    = JsonDocument.Parse(json);
            var       newDoc = doc.RootElement;
            if (newDoc.TryGetProperty("id", out var idEl) &&
                idEl.ValueKind == JsonValueKind.String &&
                Guid.TryParse(idEl.GetString(), out var newUserId))
            {
                var assignResponse = await _identity.AssignUserRoleAsync(
                    newUserId, new { roleId = roleId.Value }, token, cancellationToken);

                if (!assignResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Post-invite role assignment failed for user {UserId}: {Status}",
                        newUserId, (int)assignResponse.StatusCode);
                }
            }
        }

        return StatusCode(StatusCodes.Status201Created, json);
    }

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _admin.UpdateUserRoleAsync(id, body, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _admin.DeactivateUserAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("users/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _admin.ReactivateUserAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50,
        [FromQuery] string? from     = null,
        [FromQuery] string? to       = null,
        [FromQuery] string? action   = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _admin.GetAuditLogAsync(token, page, pageSize, from, to, action, cancellationToken), cancellationToken);
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Task<string?> GetTokenAsync() => HttpContext.GetCurrentAccessTokenAsync();

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host administration API returned {StatusCode}: {Body}",
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

