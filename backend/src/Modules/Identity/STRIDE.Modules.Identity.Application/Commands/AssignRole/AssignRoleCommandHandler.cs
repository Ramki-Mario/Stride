using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Commands.AssignRole;

internal sealed class AssignRoleCommandHandler
    : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _users  = users;
        _roles  = roles;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // ITenantContext is already set by TenantMiddleware for authenticated requests.

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("AssignRole failed: user {UserId} not found", request.UserId);
            return Result.Failure($"User '{request.UserId}' not found.");
        }

        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            _logger.LogWarning("AssignRole failed: role {RoleId} not found", request.RoleId);
            return Result.Failure($"Role '{request.RoleId}' not found.");
        }

        user.AssignRole(role, request.AssignedBy);
        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Role {RoleName} assigned to user {UserId} by {AssignedBy}",
            role.Name, user.Id, request.AssignedBy);

        return Result.Success();
    }
}
