using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Commands.AssignRole;

internal sealed class AssignRoleCommandHandler
    : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUserPermissionService _permissions;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        IUserPermissionService permissions,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _users       = users;
        _roles       = roles;
        _permissions = permissions;
        _logger      = logger;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // ITenantContext is already set by TenantMiddleware for authenticated requests.

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.AssignRoleUserNotFound(request.UserId);
            return Result.Failure($"User '{request.UserId}' not found.");
        }

        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            _logger.AssignRoleRoleNotFound(request.RoleId);
            return Result.Failure($"Role '{request.RoleId}' not found.");
        }

        user.AssignRole(role, request.AssignedBy);
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);

        // Bust the cached permission set so the new role takes effect immediately.
        _permissions.InvalidateUser(user.TenantId, user.Id);

        _logger.RoleAssigned(role.Name, user.Id, request.AssignedBy);

        return Result.Success();
    }
}
