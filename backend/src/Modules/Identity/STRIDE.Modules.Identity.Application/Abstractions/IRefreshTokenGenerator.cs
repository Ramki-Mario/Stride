namespace STRIDE.Modules.Identity.Application.Abstractions;

/// <summary>
/// Generates cryptographically random refresh token strings with their expiry.
/// Lifetime is controlled by <c>Jwt:RefreshTokenLifetimeDays</c> configuration.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Returns a new opaque token string and its UTC expiry timestamp.</summary>
    (string Token, DateTime ExpiresAt) Generate();
}
