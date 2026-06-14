using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.UpdateRole;

/// <summary>
/// Updates a custom role's name, description, and permission set.
/// System roles are immutable and will return a failure.
/// </summary>
public sealed record UpdateRoleCommand(
    Guid                 RoleId,
    string               Name,
    string               Description,
    IReadOnlyList<Guid>  PermissionIds,
    Guid                 ActorId) : IRequest<Result>;
