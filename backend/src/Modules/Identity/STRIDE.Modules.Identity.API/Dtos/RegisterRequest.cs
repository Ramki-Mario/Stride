namespace STRIDE.Modules.Identity.API.Dtos;

/// <summary>Request body for <c>POST /api/identity/auth/register</c>.</summary>
public sealed class RegisterRequest
{
    public string Email       { get; set; } = string.Empty;
    public string Password    { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
