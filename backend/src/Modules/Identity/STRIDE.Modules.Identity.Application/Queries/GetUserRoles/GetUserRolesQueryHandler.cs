using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Queries.GetUserRoles;

internal sealed class GetUserRolesQueryHandler
    : IRequestHandler<GetUserRolesQuery, Result<IReadOnlyList<UserRoleAssignmentDto>>>
{
    private readonly IUserRepository _users;

    public GetUserRolesQueryHandler(IUserRepository users) => _users = users;

    public async Task<Result<IReadOnlyList<UserRoleAssignmentDto>>> Handle(
        GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<IReadOnlyList<UserRoleAssignmentDto>>(
                $"User '{request.UserId}' not found.");

        var assignments = user.Roles
            .Where(ur => !ur.IsDeleted && ur.Role is not null)
            .Select(ur => new UserRoleAssignmentDto(
                ur.RoleId,
                ur.Role!.Name,
                ur.Role.Description,
                ur.Role.IsSystemRole))
            .ToList();

        return Result.Success<IReadOnlyList<UserRoleAssignmentDto>>(assignments);
    }
}
