namespace STRIDE.Modules.Administration.Application.DTOs;

/// <summary>
/// Returned by <see cref="ICssSanitiser.Sanitise"/> and surfaced through the
/// UpdateTenantSettings endpoint so Angular can show per-line feedback.
/// </summary>
public sealed record SanitisedCssResult(
    /// <summary>Token name → value for every accepted --stride-* assignment.</summary>
    IReadOnlyDictionary<string, string> AcceptedTokens,
    /// <summary>Raw line text for every rejected declaration, in order.</summary>
    IReadOnlyList<string> RejectedEntries)
{
    /// <summary>Serialisable JSON of the accepted tokens for DB storage.</summary>
    public string ToJson()
    {
        if (AcceptedTokens.Count == 0) return "{}";
        var pairs = AcceptedTokens
            .Select(kvp => $"\"{EscapeJson(kvp.Key)}\":\"{EscapeJson(kvp.Value)}\"");
        return "{" + string.Join(",", pairs) + "}";
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
