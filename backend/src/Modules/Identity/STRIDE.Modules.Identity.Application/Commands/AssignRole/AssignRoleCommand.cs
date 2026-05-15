using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.AssignRole;

/// <summary>
/// Assigns an existing role to a user within the current tenant.
/// Requires an authenticated admin context — <see cref="AssignedBy"/> is the
/// UserId of the administrator performing the action (populated by the
/// controller from the JWT claims in US-028).
/// </summary>
public sealed record AssignRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid AssignedBy) : IRequest<Result>;
