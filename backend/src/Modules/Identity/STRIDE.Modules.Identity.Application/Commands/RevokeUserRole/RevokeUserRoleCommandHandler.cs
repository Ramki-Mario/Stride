using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Commands.RevokeUserRole;

internal sealed class RevokeUserRoleCommandHandler
    : IRequestHandler<RevokeUserRoleCommand, Result>
{
    private readonly IUserRepository         _users;
    private readonly IUserPermissionService  _permissions;
    private readonly ILogger<RevokeUserRoleCommandHandler> _logger;

    public RevokeUserRoleCommandHandler(
        IUserRepository        users,
        IUserPermissionService permissions,
        ILogger<RevokeUserRoleCommandHandler> logger)
    {
        _users       = users;
        _permissions = permissions;
        _logger      = logger;
    }

    public async Task<Result> Handle(RevokeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("RevokeUserRole: user {UserId} not found", request.UserId);
            return Result.Failure($"User '{request.UserId}' not found.");
        }

        user.RevokeRole(request.RoleId);
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);

        _permissions.InvalidateUser(user.TenantId, user.Id);

        return Result.Success();
    }
}
