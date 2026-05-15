namespace STRIDE.Modules.Identity.Application.Queries.GetUser;

/// <summary>Read model returned by <see cref="GetUserQuery"/>.</summary>
public sealed record UserDto(
    Guid   Id,
    Guid   TenantId,
    string Email,
    string DisplayName,
    bool   IsActive,
    IReadOnlyList<string> Roles);
