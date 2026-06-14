using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// Generates cryptographically random refresh tokens encoded as lowercase hex strings (64 chars = 256 bits).
/// Expiry is computed from <see cref="JwtOptions.RefreshTokenLifetimeDays"/>.
/// </summary>
internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private readonly JwtOptions _options;

    public RefreshTokenGenerator(IOptions<JwtOptions> options)
        => _options = options.Value;

    public (string Token, DateTime ExpiresAt) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 256 bits
        var token = Convert.ToHexString(bytes).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays);
        return (token, expiresAt);
    }
}
