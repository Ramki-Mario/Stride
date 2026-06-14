using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler
    : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IRoleRepository              _roles;
    private readonly IPermissionRepository        _permissions;
    private readonly IUserPermissionService       _userPermissions;
    private readonly ITenantContext               _tenant;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IRoleRepository              roles,
        IPermissionRepository        permissions,
        IUserPermissionService       userPermissions,
        ITenantContext               tenant,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _roles           = roles;
        _permissions     = permissions;
        _userPermissions = userPermissions;
        _tenant          = tenant;
        _logger          = logger;
    }

    public async Task<Result<Guid>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        // ── Duplicate name check ──────────────────────────────────────────────
        if (await _roles.ExistsByNameAsync(request.Name.Trim().ToUpperInvariant(), cancellationToken))
            return Result.Failure<Guid>($"A role named '{request.Name}' already exists.");

        // ── No-escalation guard ───────────────────────────────────────────────
        var allPermissions = await _permissions.GetAllAsync(cancellationToken);
        var requested      = allPermissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToList();

        if (request.PermissionIds.Count > 0)
        {
            var actorKeys = await _userPermissions.GetPermissionsAsync(
                _tenant.TenantId, request.ActorId, cancellationToken);

            var escalated = requested.Where(p => !actorKeys.Contains(p.Key)).ToList();
            if (escalated.Count > 0)
            {
                _logger.LogWarning(
                    "CreateRole: actor {ActorId} attempted to grant permission(s) they don't hold: {Keys}",
                    request.ActorId,
                    string.Join(", ", escalated.Select(p => p.Key)));

                return Result.Failure<Guid>(
                    "You cannot grant a permission you do not hold: " +
                    string.Join(", ", escalated.Select(p => p.Key)));
            }
        }

        // ── Create role ───────────────────────────────────────────────────────
        var role = Role.Create(
            tenantId:    _tenant.TenantId,
            name:        request.Name,
            description: request.Description,
            createdBy:   request.ActorId);

        foreach (var permission in requested)
            role.GrantPermission(permission, request.ActorId);

        await _roles.AddAsync(role, cancellationToken);
        await _roles.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Role '{Name}' ({Id}) created in tenant {TenantId} by {ActorId}",
            role.Name, role.Id, _tenant.TenantId, request.ActorId);

        return Result.Success(role.Id);
    }
}
