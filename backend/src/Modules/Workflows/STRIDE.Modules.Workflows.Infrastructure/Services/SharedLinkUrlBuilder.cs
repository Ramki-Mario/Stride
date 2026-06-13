using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Infrastructure.Services;

/// <summary>
/// Builds public job-view URLs of the form <c>{baseUrl}/job/{token}</c>.
/// The base URL is the public SPA origin, supplied from configuration
/// (<c>PublicApp:BaseUrl</c>) at registration time.
/// </summary>
internal sealed class SharedLinkUrlBuilder : ISharedLinkUrlBuilder
{
    private readonly string _baseUrl;

    public SharedLinkUrlBuilder(string baseUrl)
        => _baseUrl = baseUrl.TrimEnd('/');

    public string BuildShareUrl(string token) => $"{_baseUrl}/job/{token}";
}
