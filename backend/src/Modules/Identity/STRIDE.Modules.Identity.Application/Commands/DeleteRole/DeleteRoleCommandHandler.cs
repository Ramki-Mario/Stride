using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Commands.DeleteRole;

internal sealed class DeleteRoleCommandHandler
    : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IRoleRepository              _roles;
    private readonly IUserPermissionService       _userPermissions;
    private readonly ITenantContext               _tenant;
    private readonly ILogger<DeleteRoleCommandHandler> _logger;

    public DeleteRoleCommandHandler(
        IRoleRepository              roles,
        IUserPermissionService       userPermissions,
        ITenantContext               tenant,
        ILogger<DeleteRoleCommandHandler> logger)
    {
        _roles           = roles;
        _userPermissions = userPermissions;
        _tenant          = tenant;
        _logger          = logger;
    }

    public async Task<Result> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            _logger.LogDebug("DeleteRole: role {RoleId} not found", request.RoleId);
            return Result.Failure($"Role '{request.RoleId}' not found.");
        }

        if (role.IsSystemRole)
            return Result.Failure("System roles cannot be deleted.");

        // Invalidate permissions for all users who hold this role before deletion.
        var affectedUserIds = await _roles.GetAssignedUserIdsAsync(request.RoleId, cancellationToken);

        role.SoftDelete();
        _roles.Update(role);
        await _roles.SaveChangesAsync(cancellationToken);

        foreach (var userId in affectedUserIds)
            _userPermissions.InvalidateUser(_tenant.TenantId, userId);

        _logger.LogInformation(
            "Role '{Name}' ({RoleId}) deleted from tenant {TenantId} by {ActorId}",
            role.Name, role.Id, _tenant.TenantId, request.ActorId);

        return Result.Success();
    }
}
