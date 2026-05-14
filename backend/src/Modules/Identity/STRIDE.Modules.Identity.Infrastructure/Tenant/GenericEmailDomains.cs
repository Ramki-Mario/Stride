namespace STRIDE.Modules.Identity.Infrastructure.Tenant;

internal static class GenericEmailDomains
{
    private static readonly HashSet<string> _domains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com",
        "outlook.com", "hotmail.com", "hotmail.co.uk", "live.com", "msn.com",
        "yahoo.com", "yahoo.co.uk", "yahoo.fr", "yahoo.de",
        "icloud.com", "me.com", "mac.com",
        "protonmail.com", "proton.me",
        "aol.com",
        "zoho.com",
        "mail.com",
        "gmx.com", "gmx.net"
    };

    public static bool IsGeneric(string domain) => _domains.Contains(domain);
}
