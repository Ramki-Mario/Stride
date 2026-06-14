using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.RevokeUserRole;

/// <summary>Revokes a specific custom role from a user within the current tenant.</summary>
public sealed record RevokeUserRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid RevokedBy) : IRequest<Result>;
