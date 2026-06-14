using Microsoft.AspNetCore.Authentication;

namespace STRIDE.BFF.Extensions;

/// <summary>
/// Extension methods for reading the current access token from HttpContext.
/// Checks the silent-renewal override in HttpContext.Items first so that when
/// the middleware has already refreshed the token during this request, the
/// controller immediately picks up the new bearer value without an extra round trip.
/// </summary>
public static class HttpContextTokenExtensions
{
    /// <summary>Key used by <see cref="SilentTokenRenewalMiddleware"/> to store the refreshed access token.</summary>
    public const string RefreshedAccessTokenKey = "__stride_refreshed_access_token__";

    /// <summary>
    /// Returns the access token for the current request.
    /// Prefers the middleware-refreshed token stored in Items over the session copy,
    /// ensuring the current request always holds a valid bearer value after renewal.
    /// </summary>
    public static Task<string?> GetCurrentAccessTokenAsync(this HttpContext context)
    {
        if (context.Items.TryGetValue(RefreshedAccessTokenKey, out var cached) && cached is string token)
            return Task.FromResult<string?>(token);

        return context.GetTokenAsync("access_token");
    }
}
