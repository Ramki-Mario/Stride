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

    /// <summary>Returns the system-wide permission catalog (for role create/edit UI).</summary>
    public Task<HttpResponseMessage> ListPermissionsAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
        => Send(HttpMethod.Get, "/api/identity/permissions", accessToken, cancellationToken);

    /// <summary>Returns all active roles for the current tenant.</summary>
    public Task<HttpResponseMessage> ListRolesAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
        => Send(HttpMethod.Get, "/api/identity/roles", accessToken, cancellationToken);

    /// <summary>Returns a single role with its active permissions.</summary>
    public Task<HttpResponseMessage> GetRoleAsync(
        Guid   roleId,
        string accessToken,
        CancellationToken cancellationToken = default)
        => Send(HttpMethod.Get, $"/api/identity/roles/{roleId}", accessToken, cancellationToken);

    /// <summary>Creates a new custom role.</summary>
    public Task<HttpResponseMessage> CreateRoleAsync(
        object body,
        string accessToken,
        CancellationToken cancellationToken = default)
        => SendJson(HttpMethod.Post, "/api/identity/roles", body, accessToken, cancellationToken);

    /// <summary>Updates an existing role's name, description, and permissions.</summary>
    public Task<HttpResponseMessage> UpdateRoleAsync(
        Guid   roleId,
        object body,
        string accessToken,
        CancellationToken cancellationToken = default)
        => SendJson(HttpMethod.Put, $"/api/identity/roles/{roleId}", body, accessToken, cancellationToken);

    /// <summary>Soft-deletes a custom role.</summary>
    public Task<HttpResponseMessage> DeleteRoleAsync(
        Guid   roleId,
        string accessToken,
        CancellationToken cancellationToken = default)
        => Send(HttpMethod.Delete, $"/api/identity/roles/{roleId}", accessToken, cancellationToken);

    private Task<HttpResponseMessage> Send(
        HttpMethod method, string path, string accessToken,
        CancellationToken cancellationToken)
    {
        var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(req, cancellationToken);
    }

    private Task<HttpResponseMessage> SendJson(
        HttpMethod method, string path, object body, string accessToken,
        CancellationToken cancellationToken)
    {
        var req = new HttpRequestMessage(method, path)
        {
            Content = System.Net.Http.Json.JsonContent.Create(body),
        };
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(req, cancellationToken);
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

    /// <summary>
    /// Validates an invite token without consuming it.
    /// Returns null on 400 (invalid/expired), the token info on success.
    /// </summary>
    public async Task<HostInviteTokenInfoResponse?> ValidateInviteTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync(
            $"/api/identity/users/accept-invite/validate?token={Uri.EscapeDataString(token)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HostInviteTokenInfoResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Validates the token, sets password, activates account, and returns a full token pair.
    /// Returns null on 400 (invalid/expired). Throws on 5xx.
    /// </summary>
    public async Task<HostLoginResponse?> AcceptInviteAsync(
        string token,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/identity/users/accept-invite",
            new { Token = token, Password = password, ConfirmPassword = confirmPassword },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HostLoginResponse>(cancellationToken: cancellationToken);
    }
}
