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
}
