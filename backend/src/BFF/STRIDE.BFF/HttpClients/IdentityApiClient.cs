using System.Net;
using System.Net.Http.Json;
using STRIDE.BFF.Auth;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Identity module surface of STRIDE.Host (ADR-011).
/// Used by the BFF to authenticate users and exchange the resulting JWT for
/// an HttpOnly Redis-backed session cookie.
///
/// NOTE: The Host endpoint <c>POST /api/identity/auth/login</c> is implemented
/// in US-028 (controllers) backed by US-026 (LoginCommand handler).
/// </summary>
public sealed class IdentityApiClient
{
    private readonly HttpClient _client;

    public IdentityApiClient(HttpClient client) => _client = client;

    /// <summary>
    /// Returns all active roles for the current tenant.
    /// Forwards the Bearer token from the BFF session cookie.
    /// </summary>
    public Task<HttpResponseMessage> ListRolesAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/roles");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Authenticates the user against the Host. Returns null on 401/invalid credentials,
    /// the populated response on success, and throws on transport / 5xx errors.
    /// </summary>
    public async Task<HostLoginResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/identity/auth/login",
            new { request.Email, request.Password },
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HostLoginResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Rotates a refresh token. Returns null when the Host rejects the token (401),
    /// the new token pair on success, and throws on transport / 5xx errors.
    /// </summary>
    public async Task<HostTokenPairResponse?> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/identity/auth/refresh",
            new { Token = refreshToken },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HostTokenPairResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Revokes a refresh token on the Host (fire-and-forget safe — idempotent).
    /// Throws on transport / 5xx errors; 404 is swallowed (token already gone).
    /// </summary>
    public async Task RevokeTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/identity/auth/revoke",
            new { Token = refreshToken },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return; // already revoked/gone — OK
        response.EnsureSuccessStatusCode();
    }
}
