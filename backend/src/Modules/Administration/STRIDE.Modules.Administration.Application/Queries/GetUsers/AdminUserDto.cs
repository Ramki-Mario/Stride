namespace STRIDE.Modules.Administration.Application.Queries.GetUsers;

/// <summary>Read-model for a single user row on the Administration Users page.</summary>
public sealed record AdminUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Role,
    bool IsActive,
    bool IsPending,
    DateTime CreatedAt);
