using System.Text.RegularExpressions;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Application.DTOs;

namespace STRIDE.Modules.Administration.Infrastructure.Services;

/// <summary>
/// Deny-by-default CSS sanitiser.
///
/// Rules:
///   ✅  <c>--stride-*</c> custom property declarations inside <c>:root {}</c>
///   ❌  Any value containing <c>url()</c>, <c>expression()</c>, <c>@import</c>, or <c>&lt;script&gt;</c>
///   ❌  Custom properties not prefixed <c>--stride-</c>
///   ❌  Any selector other than <c>:root</c>
///   ❌  Declarations outside a <c>:root</c> block
/// </summary>
internal sealed partial class CssSanitiser : ICssSanitiser
{
    // Matches a single :root { ... } block (handles multi-line, non-greedy).
    [GeneratedRegex(@":root\s*\{([^}]*)\}", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex RootBlockRegex();

    // Matches a CSS custom property declaration: --name : value ;
    [GeneratedRegex(@"(--[a-zA-Z0-9-]+)\s*:\s*([^;]+?)\s*;", RegexOptions.Singleline)]
    private static partial Regex DeclarationRegex();

    // Patterns that are never safe in a CSS value.
    [GeneratedRegex(@"url\s*\(|expression\s*\(|@import|<\s*script", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousValueRegex();

    // Strip block and line comments before processing.
    [GeneratedRegex(@"/\*.*?\*/|//[^\r\n]*", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    public SanitisedCssResult Sanitise(string rawCss)
    {
        if (string.IsNullOrWhiteSpace(rawCss))
            return new SanitisedCssResult(
                new Dictionary<string, string>(),
                Array.Empty<string>());

        var accepted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rejected = new List<string>();

        // 1. Strip comments.
        var cleaned = CommentRegex().Replace(rawCss, string.Empty);

        // 2. Extract all :root { } blocks.
        var rootMatches = RootBlockRegex().Matches(cleaned);

        if (rootMatches.Count == 0)
        {
            // No :root block found — every line the user wrote is "rejected".
            CollectAllDeclarationsAsRejected(cleaned, rejected);
            return new SanitisedCssResult(accepted, rejected);
        }

        // 3. Collect the indices of content that IS inside :root blocks.
        var rootContent = string.Concat(rootMatches.Select(m => m.Groups[1].Value));

        // 4. Content outside :root blocks — all declarations there are rejected.
        var outsideRootCss = RootBlockRegex().Replace(cleaned, string.Empty);
        CollectAllDeclarationsAsRejected(outsideRootCss, rejected);

        // 5. Parse declarations inside :root.
        foreach (Match decl in DeclarationRegex().Matches(rootContent))
        {
            var name  = decl.Groups[1].Value.Trim();
            var value = decl.Groups[2].Value.Trim();
            var raw   = decl.Value.Trim();

            // Must be a --stride-* property.
            if (!name.StartsWith("--stride-", StringComparison.OrdinalIgnoreCase))
            {
                rejected.Add(raw);
                continue;
            }

            // Value must not contain dangerous patterns.
            if (DangerousValueRegex().IsMatch(value))
            {
                rejected.Add(raw);
                continue;
            }

            // Last-write-wins if the same token appears twice.
            accepted[name] = value;
        }

        return new SanitisedCssResult(accepted, rejected);
    }

    private static void CollectAllDeclarationsAsRejected(string css, List<string> rejected)
    {
        foreach (Match m in DeclarationRegex().Matches(css))
            rejected.Add(m.Value.Trim());
    }
}
