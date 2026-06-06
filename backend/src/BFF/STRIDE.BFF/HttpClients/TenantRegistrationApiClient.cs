using System.Net.Http.Json;
using STRIDE.BFF.Auth;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the tenant self-registration surface of STRIDE.Host.
///
///   POST /api/identity/tenants/register → creates Tenant + first admin user,
///   returns the same JWT shape as /api/identity/auth/login.
/// </summary>
public sealed class TenantRegistrationApiClient
{
    private readonly HttpClient _client;

    public TenantRegistrationApiClient(HttpClient client) => _client = client;

    public async Task<HostLoginResponse?> RegisterAsync(
        object request,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/identity/tenants/register", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Tenant registration failed ({(int)response.StatusCode}): {body}",
                null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<HostLoginResponse>(cancellationToken: cancellationToken);
    }
}
