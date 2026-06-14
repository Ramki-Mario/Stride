using System.ComponentModel.DataAnnotations;

namespace STRIDE.Modules.Identity.API.Dtos;

public sealed record CreateRoleRequest(
    [Required, MaxLength(100)] string              Name,
    [MaxLength(500)]           string              Description,
                               IReadOnlyList<Guid> PermissionIds);
