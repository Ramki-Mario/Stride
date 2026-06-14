namespace STRIDE.Modules.Identity.API.Dtos;

/// <summary>Request body for token refresh and revoke endpoints.</summary>
public sealed class TokenRequest
{
    public string Token { get; set; } = string.Empty;
}
