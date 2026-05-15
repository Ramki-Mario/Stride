namespace STRIDE.Modules.Identity.Application.Abstractions;

/// <summary>
/// Mints signed JWT access tokens for authenticated users.
/// Implemented in Identity.Infrastructure using HS256 with a configurable secret.
/// Tokens carry standard claims (sub, email, jti) plus the custom <c>tid</c> tenant claim
/// and a <c>role</c> claim per assigned role.
/// </summary>
public interface IJwtTokenService
{
    JwtTokenResult Generate(JwtTokenRequest request);
}

public sealed record JwtTokenRequest(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);
