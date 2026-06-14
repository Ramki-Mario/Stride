namespace STRIDE.Modules.Identity.Application.Queries.GetUserRoles;

/// <summary>A single custom-role assignment for a user.</summary>
public sealed record UserRoleAssignmentDto(
    Guid   RoleId,
    string Name,
    string Description,
    bool   IsSystemRole);
