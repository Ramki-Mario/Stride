using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Auth;
using STRIDE.BFF.HttpClients;
using System.Security.Claims;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for self-service tenant registration.
///
/// POST /bff/tenants/register
///   Forwards the registration payload to the Host, then — on success — creates
///   an HttpOnly Redis-backed session cookie exactly as /bff/auth/login does.
///   The Angular SPA calls this endpoint anonymously and is redirected to
///   /dashboard on completion.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("bff/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly TenantRegistrationApiClient _registration;
    private readonly TenantSettingsApiClient     _tenantSettings;
    private readonly ILogger<TenantsController>  _logger;

    public TenantsController(
        TenantRegistrationApiClient registration,
        TenantSettingsApiClient     tenantSettings,
        ILogger<TenantsController>  logger)
    {
        _registration   = registration;
        _tenantSettings = tenantSettings;
        _logger         = logger;
    }

    public sealed class RegisterTenantBffRequest
    {
        [Required] public string OrgName          { get; set; } = string.Empty;
        [Required] public string Slug             { get; set; } = string.Empty;
        [Required] public string Plan             { get; set; } = "Starter";
        [Required, EmailAddress] public string AdminEmail       { get; set; } = string.Empty;
        [Required] public string AdminPassword    { get; set; } = string.Empty;
        [Required] public string AdminDisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// POST /bff/tenants/register
    /// Registers the org, auto-creates a session cookie, and returns the Me payload.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterTenantBffRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        HostLoginResponse? hostResponse;
        try
        {
            hostResponse = await _registration.RegisterAsync(new
            {
                request.OrgName,
                request.Slug,
                request.Plan,
                request.AdminEmail,
                request.AdminPassword,
                request.AdminDisplayName,
            }, ct);
        }
        catch (HttpRequestException ex) when ((int?)ex.StatusCode is 400 or 409)
        {
            _logger.LogWarning(ex, "Tenant registration rejected by Host.");
            return StatusCode((int)ex.StatusCode!.Value, new ProblemDetails
            {
                Title  = ex.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? "Slug already taken"
                    : "Registration failed",
                Detail = ex.Message,
                Status = (int)ex.StatusCode.Value,
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Host identity API unreachable during tenant registration.");
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title  = "Identity service unavailable",
                Detail = "Unable to reach the identity service. Please try again shortly.",
                Status = StatusCodes.Status502BadGateway,
            });
        }

        if (hostResponse is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Unexpected registration error"
            });

        // ── Issue session cookie — identical to /bff/auth/login ──────────────
        var principal  = BuildPrincipal(hostResponse);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc    = DateTimeOffset.UtcNow,
            ExpiresUtc   = new DateTimeOffset(hostResponse.ExpiresAtUtc, TimeSpan.Zero),
            AllowRefresh = false,
        };
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = hostResponse.AccessToken },
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        // Fetch defaultPalette — new tenant defaults to "purple"; ignore any error
        string defaultPalette = "purple";
        try
        {
            var settingsResp = await _tenantSettings.GetSettingsAsync(hostResponse.AccessToken, ct);
            if (settingsResp.IsSuccessStatusCode)
            {
                var json = await settingsResp.Content.ReadAsStringAsync(ct);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("defaultPalette", out var p))
                    defaultPalette = p.GetString() ?? "purple";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch defaultPalette after tenant registration.");
        }

        _logger.LogInformation(
            "[BFF] Tenant registration complete — userId={UserId} tenantId={TenantId}",
            hostResponse.UserId, hostResponse.TenantId);

        return Ok(new MeResponse(
            hostResponse.UserId,
            hostResponse.TenantId,
            hostResponse.Email,
            hostResponse.DisplayName,
            hostResponse.Roles,
            defaultPalette));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildPrincipal(HostLoginResponse response)
    {
        var claims = new List<Claim>
        {
            new("sub", response.UserId.ToString("N")),
            new("tid", response.TenantId.ToString("N")),
            new(ClaimTypes.NameIdentifier, response.UserId.ToString("N")),
            new(ClaimTypes.Email,          response.Email),
            new(ClaimTypes.Name,           response.DisplayName),
        };
        claims.AddRange(response.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
