namespace STRIDE.BFF.Auth;

/// <summary>Controls the BFF silent token renewal middleware.</summary>
public sealed class SilentTokenRenewalOptions
{
    public const string SectionName = "SilentTokenRenewal";

    /// <summary>
    /// How many minutes before the access token expires to proactively refresh it.
    /// A request that arrives within this window triggers a background rotation
    /// so the CURRENT request still uses a valid token (stored in HttpContext.Items).
    /// Default: 5 minutes.
    /// </summary>
    public int RefreshSkewMinutes { get; set; } = 5;
}
