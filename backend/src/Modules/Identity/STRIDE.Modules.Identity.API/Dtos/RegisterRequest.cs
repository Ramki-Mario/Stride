namespace STRIDE.Modules.Identity.API.Dtos;

/// <summary>Request body for <c>POST /api/identity/auth/register</c>.</summary>
public sealed class RegisterRequest
{
    public required string Email       { get; set; }
    public required string Password    { get; set; }
    public required string DisplayName { get; set; }
}
