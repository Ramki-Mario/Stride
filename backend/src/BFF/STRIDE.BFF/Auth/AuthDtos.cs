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
/// Response from the Host Identity API's login endpoint.
/// (Host endpoint comes in US-028 / US-026 — for now this is the contract.)
/// </summary>
public sealed record HostLoginResponse(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTime ExpiresAtUtc);

/// <summary>Response body for <c>GET /bff/auth/me</c>.</summary>
public sealed record MeResponse(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string DefaultPalette = "purple",
    string TenantName     = "");
