using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Auth;
using STRIDE.BFF.Extensions;
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
    private const string DefaultPalette    = "purple";
    private const string RefreshTokenName  = "refresh_token";

    private readonly IdentityApiClient       _identity;
    private readonly TenantSettingsApiClient _tenantSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IdentityApiClient       identity,
        TenantSettingsApiClient tenantSettings,
        ILogger<AuthController> logger)
    {
        _identity       = identity;
        _tenantSettings = tenantSettings;
        _logger         = logger;
    }

    /// <summary>
    /// POST /bff/auth/login. Forwards credentials to Host, then stores the resulting
    /// JWT in a Redis-backed session and sets an HttpOnly cookie on the response.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        HostLoginResponse? hostResponse;
        try
        {
            hostResponse = await _identity.LoginAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Host identity API unreachable during login.");
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title  = "Identity service unavailable",
                Detail = "Unable to reach the identity service. Please try again shortly.",
                Status = StatusCodes.Status502BadGateway
            });
        }

        if (hostResponse is null)
            return Unauthorized(new ProblemDetails
            {
                Title  = "Invalid credentials",
                Status = StatusCodes.Status401Unauthorized
            });

        var principal  = BuildPrincipal(hostResponse);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc    = DateTimeOffset.UtcNow,
            ExpiresUtc   = new DateTimeOffset(hostResponse.RefreshTokenExpiresAtUtc, TimeSpan.Zero),
            AllowRefresh = false
        };
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token",               Value = hostResponse.AccessToken },
            new AuthenticationToken { Name = RefreshTokenName,              Value = hostResponse.RefreshToken },
            new AuthenticationToken { Name = "access_token_expires_at",    Value = hostResponse.AccessTokenExpiresAtUtc.ToString("O") },
            new AuthenticationToken { Name = "refresh_token_expires_at",   Value = hostResponse.RefreshTokenExpiresAtUtc.ToString("O") },
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        // Fetch tenant settings immediately after login so the login response is complete.
        var (defaultPalette, tenantName) = await GetTenantSettingsAsync(hostResponse.AccessToken, cancellationToken);

        return Ok(ToMeResponse(hostResponse, defaultPalette, tenantName));
    }

    /// <summary>
    /// POST /bff/auth/logout. Revokes the server-side refresh token, removes the Redis
    /// session, and clears the cookie. Revocation is best-effort — the session is cleared
    /// regardless of whether the Host call succeeds.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = await HttpContext.GetTokenAsync(RefreshTokenName);
        if (refreshToken is not null)
        {
            try { await _identity.RevokeTokenAsync(refreshToken, cancellationToken); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke refresh token on logout — session will still be cleared.");
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>
    /// POST /bff/auth/refresh. Rotates the refresh token and updates the session with
    /// the new token pair. Called by Angular (or the US-118 middleware) when the access
    /// token has expired. Uses the refresh token stored in the HttpOnly session cookie
    /// as the credential — no Bearer header required.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = await HttpContext.GetTokenAsync(RefreshTokenName);
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        HostTokenPairResponse? tokenPair;
        try
        {
            tokenPair = await _identity.RefreshTokenAsync(refreshToken, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Host identity API unreachable during token refresh.");
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title  = "Identity service unavailable",
                Detail = "Unable to reach the identity service. Please try again shortly.",
                Status = StatusCodes.Status502BadGateway
            });
        }

        if (tokenPair is null)
            return Unauthorized();

        // Re-authenticate: persist the rotated token pair into the session cookie.
        var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = authResult?.Properties ?? new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = false
        };

        properties.ExpiresUtc = new DateTimeOffset(tokenPair.RefreshTokenExpiresAtUtc, TimeSpan.Zero);
        properties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token",             Value = tokenPair.AccessToken },
            new AuthenticationToken { Name = RefreshTokenName,            Value = tokenPair.RefreshToken },
            new AuthenticationToken { Name = "access_token_expires_at",  Value = tokenPair.AccessTokenExpiresAtUtc.ToString("O") },
            new AuthenticationToken { Name = "refresh_token_expires_at", Value = tokenPair.RefreshTokenExpiresAtUtc.ToString("O") },
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            HttpContext.User,
            properties);

        return NoContent();
    }

    /// <summary>
    /// GET /bff/auth/me. Returns the current user's session info including defaultPalette.
    /// Called by Angular on app boot (APP_INITIALIZER) and after any 401.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = HttpContext.User;
        if (!TryParseGuidClaim(user, "sub", out var userId) ||
            !TryParseGuidClaim(user, "tid", out _))
        {
            return Unauthorized();
        }

        var email       = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var displayName = user.FindFirst(ClaimTypes.Name)?.Value  ?? string.Empty;
        var roles       = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var tenantId    = GetTenantIdFromClaims(user);

        var token = await HttpContext.GetCurrentAccessTokenAsync();
        var (defaultPalette, tenantName) = token is not null
            ? await GetTenantSettingsAsync(token, cancellationToken)
            : (DefaultPalette, "");

        return Ok(new MeResponse(userId, tenantId, email, displayName, roles, defaultPalette, tenantName));
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches TenantSettings and extracts defaultPalette + displayName in a single call.
    /// Returns safe defaults on any failure so auth never breaks due to a settings fault.
    /// </summary>
    private async Task<(string Palette, string TenantName)> GetTenantSettingsAsync(
        string token, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _tenantSettings.GetSettingsAsync(token, cancellationToken);
            if (!response.IsSuccessStatusCode) return (DefaultPalette, "");

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var palette = root.TryGetProperty("defaultPalette", out var paletteProp)
                ? paletteProp.GetString() ?? DefaultPalette
                : DefaultPalette;

            var name = root.TryGetProperty("displayName", out var nameProp)
                ? nameProp.GetString() ?? ""
                : "";

            return (palette, name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch tenant settings for /auth/me — using defaults.");
            return (DefaultPalette, "");
        }
    }

    private static Guid GetTenantIdFromClaims(ClaimsPrincipal user)
    {
        TryParseGuidClaim(user, "tid", out var tenantId);
        return tenantId;
    }

    private static ClaimsPrincipal BuildPrincipal(HostLoginResponse response)
    {
        var claims = new List<Claim>
        {
            new("sub", response.UserId.ToString("N")),
            new("tid", response.TenantId.ToString("N")),
            new(ClaimTypes.NameIdentifier, response.UserId.ToString("N")),
            new(ClaimTypes.Email,          response.Email),
            new(ClaimTypes.Name,           response.DisplayName)
        };
        claims.AddRange(response.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static MeResponse ToMeResponse(HostLoginResponse r, string defaultPalette, string tenantName) =>
        new(r.UserId, r.TenantId, r.Email, r.DisplayName, r.Roles, defaultPalette, tenantName);

    private static bool TryParseGuidClaim(ClaimsPrincipal user, string claimType, out Guid value)
    {
        value = Guid.Empty;
        var raw = user.FindFirst(claimType)?.Value;
        return !string.IsNullOrEmpty(raw) && Guid.TryParseExact(raw, "N", out value);
    }
}
