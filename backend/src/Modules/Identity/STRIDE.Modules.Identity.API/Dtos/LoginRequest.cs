namespace STRIDE.Modules.Identity.API.Dtos;

/// <summary>Request body for <c>POST /api/identity/auth/login</c>.</summary>
/// <remarks>
/// Structural validation (empty fields, email format, length) is enforced
/// by <c>LoginCommandValidator</c> via the MediatR validation pipeline.
/// No DataAnnotations are needed here.
/// </remarks>
public sealed class LoginRequest
{
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
