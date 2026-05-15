namespace STRIDE.Modules.Identity.Application.Commands.LoginUser;

/// <summary>
/// Returned by <see cref="LoginCommand"/> on successful authentication.
/// The <see cref="AccessToken"/> is forwarded to the BFF which exchanges it
/// for an HttpOnly session cookie backed by Redis (ADR-007).
/// </summary>
public sealed record LoginResult(
    Guid   UserId,
    Guid   TenantId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTime ExpiresAtUtc);
