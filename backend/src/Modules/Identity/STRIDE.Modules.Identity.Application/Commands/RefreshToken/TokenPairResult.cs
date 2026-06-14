namespace STRIDE.Modules.Identity.Application.Commands.RefreshToken;

/// <summary>Returned when an access+refresh token pair is successfully issued or rotated.</summary>
public sealed record TokenPairResult(
    string   AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string   RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
