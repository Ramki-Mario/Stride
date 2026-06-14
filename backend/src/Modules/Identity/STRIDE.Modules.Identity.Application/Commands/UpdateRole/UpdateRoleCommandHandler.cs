using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler
    : IRequestHandler<UpdateRoleCommand, Result>
{
    private readonly IRoleRepository              _roles;
    private readonly IPermissionRepository        _permissions;
    private readonly IUserPermissionService       _userPermissions;
    private readonly ITenantContext               _tenant;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IRoleRepository              roles,
        IPermissionRepository        permissions,
        IUserPermissionService       userPermissions,
        ITenantContext               tenant,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _roles           = roles;
        _permissions     = permissions;
        _userPermissions = userPermissions;
        _tenant          = tenant;
        _logger          = logger;
    }

    public async Task<Result> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            _logger.LogDebug("UpdateRole: role {RoleId} not found", request.RoleId);
            return Result.Failure($"Role '{request.RoleId}' not found.");
        }

        if (role.IsSystemRole)
            return Result.Failure("System roles cannot be modified.");

        // ── Duplicate name check (only when name actually changes) ────────────
        var newNormalised = request.Name.Trim().ToUpperInvariant();
        if (newNormalised != role.NormalizedName &&
            await _roles.ExistsByNameAsync(newNormalised, cancellationToken))
        {
            return Result.Failure($"A role named '{request.Name}' already exists.");
        }

        // ── No-escalation guard ───────────────────────────────────────────────
        var allPermissions = await _permissions.GetAllAsync(cancellationToken);
        var desired        = allPermissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToList();

        if (desired.Count > 0)
        {
            var actorKeys = await _userPermissions.GetPermissionsAsync(
                _tenant.TenantId, request.ActorId, cancellationToken);

            var escalated = desired.Where(p => !actorKeys.Contains(p.Key)).ToList();
            if (escalated.Count > 0)
            {
                _logger.LogWarning(
                    "UpdateRole: actor {ActorId} attempted to grant permission(s) they don't hold: {Keys}",
                    request.ActorId,
                    string.Join(", ", escalated.Select(p => p.Key)));

                return Result.Failure(
                    "You cannot grant a permission you do not hold: " +
                    string.Join(", ", escalated.Select(p => p.Key)));
            }
        }

        // ── Apply changes ─────────────────────────────────────────────────────
        role.Update(request.Name, request.Description);
        role.SyncPermissions(desired, request.ActorId);

        _roles.Update(role);
        await _roles.SaveChangesAsync(cancellationToken);

        // Invalidate cached permissions for all users assigned to this role.
        var affectedUserIds = await _roles.GetAssignedUserIdsAsync(request.RoleId, cancellationToken);
        foreach (var userId in affectedUserIds)
            _userPermissions.InvalidateUser(_tenant.TenantId, userId);

        _logger.LogInformation(
            "Role '{Name}' ({RoleId}) updated in tenant {TenantId} by {ActorId}",
            role.Name, role.Id, _tenant.TenantId, request.ActorId);

        return Result.Success();
    }
}
