namespace STRIDE.Modules.Identity.Application.Commands.RegisterUser;

/// <summary>Returned by <see cref="RegisterCommand"/> on successful user creation.</summary>
public sealed record RegisterResult(
    Guid   UserId,
    Guid   TenantId,
    string Email,
    string DisplayName);
