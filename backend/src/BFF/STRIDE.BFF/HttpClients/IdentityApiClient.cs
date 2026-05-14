namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Identity module surface of STRIDE.Host.
/// Full implementation in Phase 2 (auth flow).
/// </summary>
public sealed class IdentityApiClient
{
    private readonly HttpClient _client;

    public IdentityApiClient(HttpClient client) => _client = client;
}
