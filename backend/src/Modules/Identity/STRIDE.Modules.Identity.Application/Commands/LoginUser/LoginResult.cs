namespace STRIDE.Modules.Identity.Application.Commands.LoginUser;

/// <summary>
/// Returned by <see cref="LoginCommand"/> on successful authentication.
/// Contains both an access token (short-lived JWT) and a refresh token (long-lived opaque token).
/// The BFF stores the refresh token in an HttpOnly cookie and uses it to silently renew sessions.
/// </summary>
public sealed record LoginResult(
    Guid   UserId,
    Guid   TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string   AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string   RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    bool     IsSuperAdmin = false);
