namespace STRIDE.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// Configuration for signing JWT access tokens. Bound from the "Jwt" configuration section.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 signing secret. MUST be at least 32 bytes (256 bits) of entropy.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Token <c>iss</c> claim.</summary>
    public string Issuer { get; set; } = "STRIDE";

    /// <summary>Token <c>aud</c> claim — set to the API consumer audience.</summary>
    public string Audience { get; set; } = "STRIDE.Clients";

    /// <summary>Access token lifetime in minutes. Default: 8 hours.</summary>
    public int ExpiryMinutes { get; set; } = 480;
}
