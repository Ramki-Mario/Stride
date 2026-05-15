using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Auth;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF auth surface called by the Angular SPA. Never exposes the JWT directly —
/// tokens are kept server-side in Redis, the browser only sees an opaque HttpOnly cookie.
/// </summary>
[ApiController]
[Route("bff/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IdentityApiClient _identity;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IdentityApiClient identity, ILogger<AuthController> logger)
    {
        _identity = identity;
        _logger = logger;
    }

    /// <summary>
    /// POST /bff/auth/login. Forwards credentials to Host, then stores the resulting
    /// JWT in a Redis-backed session and sets an HttpOnly cookie on the response.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        HostLoginResponse? hostResponse;
        try
        {
            hostResponse = await _identity.LoginAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Host identity API unreachable during login.");
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "Identity service unavailable",
                Detail = "Unable to reach the identity service. Please try again shortly.",
                Status = StatusCodes.Status502BadGateway
            });
        }

        if (hostResponse is null)
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Status = StatusCodes.Status401Unauthorized
            });

        var principal = BuildPrincipal(hostResponse);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = new DateTimeOffset(hostResponse.ExpiresAtUtc, TimeSpan.Zero),
            AllowRefresh = false
        };
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = hostResponse.AccessToken }
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        return Ok(ToMeResponse(hostResponse));
    }

    /// <summary>
    /// POST /bff/auth/logout. Removes the Redis session and clears the cookie.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>
    /// GET /bff/auth/me. Returns the current user's session info. Used by Angular
    /// on app boot (APP_INITIALIZER) and after any 401.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var user = HttpContext.User;
        if (!TryParseGuidClaim(user, "sub", out var userId) ||
            !TryParseGuidClaim(user, "tid", out var tenantId))
        {
            return Unauthorized();
        }

        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var displayName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Ok(new MeResponse(userId, tenantId, email, displayName, roles));
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildPrincipal(HostLoginResponse response)
    {
        var claims = new List<Claim>
        {
            new("sub", response.UserId.ToString("N")),
            new("tid", response.TenantId.ToString("N")),
            new(ClaimTypes.NameIdentifier, response.UserId.ToString("N")),
            new(ClaimTypes.Email, response.Email),
            new(ClaimTypes.Name, response.DisplayName)
        };
        claims.AddRange(response.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static MeResponse ToMeResponse(HostLoginResponse r) =>
        new(r.UserId, r.TenantId, r.Email, r.DisplayName, r.Roles);

    private static bool TryParseGuidClaim(ClaimsPrincipal user, string claimType, out Guid value)
    {
        value = Guid.Empty;
        var raw = user.FindFirst(claimType)?.Value;
        return !string.IsNullOrEmpty(raw) && Guid.TryParseExact(raw, "N", out value);
    }
}
