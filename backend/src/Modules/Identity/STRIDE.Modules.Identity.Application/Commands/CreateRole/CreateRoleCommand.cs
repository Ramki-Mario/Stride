using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.CreateRole;

/// <summary>
/// Creates a new custom role for the current tenant and grants the specified permissions.
/// <c>ActorId</c> is the calling user — enforced against the no-escalation guard.
/// </summary>
public sealed record CreateRoleCommand(
    string               Name,
    string               Description,
    IReadOnlyList<Guid>  PermissionIds,
    Guid                 ActorId) : IRequest<Result<Guid>>;
