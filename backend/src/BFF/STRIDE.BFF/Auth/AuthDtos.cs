using System.ComponentModel.DataAnnotations;

namespace STRIDE.BFF.Auth;

/// <summary>Request body for <c>POST /bff/auth/login</c>.</summary>
public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response from the Host Identity API's login and token-refresh endpoints.
/// Carries both the short-lived access token (JWT) and the long-lived refresh token (opaque).
/// </summary>
public sealed record HostLoginResponse(
    Guid   UserId,
    Guid   TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string   AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string   RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>
/// Response from the Host Identity API's token-refresh endpoint.
/// Mirrors <c>TokenPairResult</c> in STRIDE.Modules.Identity.Application.
/// </summary>
public sealed record HostTokenPairResponse(
    string   AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string   RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>Response body for <c>GET /bff/auth/me</c>.</summary>
public sealed record MeResponse(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string DefaultPalette = "purple",
    string TenantName     = "");

/// <summary>Response from the Host's invite-token validation endpoint.</summary>
public sealed record HostInviteTokenInfoResponse(string Email, string DisplayName);

/// <summary>Request body for <c>POST /bff/auth/accept-invite</c>.</summary>
public sealed class AcceptInviteRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
