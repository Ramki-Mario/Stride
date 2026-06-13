namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Builds the full, public-facing shareable URL for a link token (e.g.
/// <c>https://app.example.com/job/{token}</c>). The base origin is environment
/// configuration, so it is resolved in Infrastructure rather than hard-coded here.
/// </summary>
public interface ISharedLinkUrlBuilder
{
    string BuildShareUrl(string token);
}
