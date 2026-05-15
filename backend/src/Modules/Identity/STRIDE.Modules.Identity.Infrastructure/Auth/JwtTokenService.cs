using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// HS256-signed JWT issuer. Reads its configuration from <see cref="JwtOptions"/>.
/// Tokens are minted by the Host (via the LoginCommand handler in US-026) and forwarded
/// to the BFF, which exchanges them for an HttpOnly cookie + Redis session.
/// </summary>
internal sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Secret))
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set it via appsettings or environment variable.");

        var keyBytes = Encoding.UTF8.GetBytes(_options.Secret);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 bytes (256 bits) of entropy for HS256.");

        var key = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public JwtTokenResult Generate(JwtTokenRequest request)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString("N")),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new(JwtRegisteredClaimNames.Name, request.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),

            // Custom tenant claim — consumed by TenantMiddleware on the Host
            new("tid", request.TenantId.ToString("N"))
        };

        claims.AddRange(request.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtTokenResult(accessToken, expiresAt);
    }
}
