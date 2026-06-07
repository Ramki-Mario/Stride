namespace STRIDE.Modules.Identity.API.Dtos;

/// <summary>Request body for <c>POST /api/identity/users/{userId}/roles</c>.</summary>
public sealed class AssignRoleRequest
{
    public required Guid RoleId { get; set; }
}
