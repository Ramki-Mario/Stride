using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using STRIDE.BFF.Auth;
using STRIDE.BFF.Controllers;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Middleware;

/// <summary>
/// Proactively rotates the access token before it expires so that BFF proxy controllers
/// always forward a valid bearer to the Host (ADR-007 / ADR-011).
///
/// Behaviour:
///   1. Skips the /bff/auth path segment (login, logout, refresh) to avoid loops.
///   2. If the access token expires within <see cref="SilentTokenRenewalOptions.RefreshSkewMinutes"/>
///      (or is already expired), acquires a per-user semaphore and calls the Host refresh endpoint.
///   3. On success: persists the new token pair to the Redis session via SignInAsync, and stores
///      the new access token in <see cref="HttpContextTokenExtensions.RefreshedAccessTokenKey"/>
///      so the CURRENT request's controller can immediately use the fresh bearer value.
///   4. On failure (Host 401, network error): logs a warning and continues — the controller
///      will surface a 401 to Angular, which is expected to retry or re-authenticate.
///   5. A <see cref="TokenRenewalLockProvider"/> semaphore per user prevents parallel requests
///      from triggering simultaneous rotations. After acquiring the lock the expiry is re-read
///      from the session so a concurrent refresh that just completed is honoured.
/// </summary>
public sealed class SilentTokenRenewalMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SilentTokenRenewalMiddleware> _logger;

    public SilentTokenRenewalMiddleware(
        RequestDelegate next,
        ILogger<SilentTokenRenewalMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IdentityApiClient      identityClient,
        TokenRenewalLockProvider lockProvider,
        IOptions<SilentTokenRenewalOptions> options)
    {
        // Skip auth paths (login, logout, refresh) — would cause infinite loops.
        if (context.Request.Path.StartsWithSegments("/bff/auth", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Only attempt renewal for authenticated sessions.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var skew = TimeSpan.FromMinutes(options.Value.RefreshSkewMinutes);

        if (!IsExpiringWithin(await context.GetTokenAsync("access_token_expires_at"), skew))
        {
            await _next(context);
            return;
        }

        // Identify the user for the per-user lock.
        var userIdRaw = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParseExact(userIdRaw, "N", out var userId))
        {
            await _next(context);
            return;
        }

        var semaphore = lockProvider.GetOrCreate(userId);

        // Try to acquire without blocking indefinitely — fall through on timeout.
        if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(5), context.RequestAborted))
        {
            _logger.SilentRenewalLockTimeout(userId);
            await _next(context);
            return;
        }

        try
        {
            // Re-check after acquiring: a concurrent request may have already refreshed.
            if (!IsExpiringWithin(await context.GetTokenAsync("access_token_expires_at"), skew))
            {
                // Token was refreshed while we waited — grab the fresh value from the session.
                var sessionToken = await context.GetTokenAsync("access_token");
                if (sessionToken is not null)
                    context.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey] = sessionToken;

                await _next(context);
                return;
            }

            var refreshToken = await context.GetTokenAsync("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
            {
                await _next(context);
                return;
            }

            HostTokenPairResponse? tokenPair;
            try
            {
                tokenPair = await identityClient.RefreshTokenAsync(refreshToken, context.RequestAborted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.SilentRenewalNetworkError(userId, ex);
                await _next(context);
                return;
            }

            if (tokenPair is null)
            {
                _logger.SilentRenewalFailed(userId);
                await _next(context);
                return;
            }

            // Persist the new token pair into the Redis session (for subsequent requests).
            var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = authResult?.Properties ?? new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = false
            };

            properties.ExpiresUtc = new DateTimeOffset(tokenPair.RefreshTokenExpiresAtUtc, TimeSpan.Zero);
            properties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token",             Value = tokenPair.AccessToken },
                new AuthenticationToken { Name = "refresh_token",            Value = tokenPair.RefreshToken },
                new AuthenticationToken { Name = "access_token_expires_at",  Value = tokenPair.AccessTokenExpiresAtUtc.ToString("O") },
                new AuthenticationToken { Name = "refresh_token_expires_at", Value = tokenPair.RefreshTokenExpiresAtUtc.ToString("O") },
            });

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                context.User,
                properties);

            // Override the in-request token so controllers get the fresh bearer immediately.
            context.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey] = tokenPair.AccessToken;

            _logger.SilentRenewalSucceeded(userId);
        }
        finally
        {
            semaphore.Release();
        }

        await _next(context);
    }

    private static bool IsExpiringWithin(string? expiresAtStr, TimeSpan skew)
    {
        if (!DateTime.TryParse(expiresAtStr, null, DateTimeStyles.RoundtripKind, out var expiresAt))
            return false;

        return expiresAt.ToUniversalTime() - DateTime.UtcNow <= skew;
    }
}
