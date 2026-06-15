using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Queries.GetRole;

namespace STRIDE.Modules.Identity.Application.Queries.GetMyRoles;

internal sealed class GetMyRolesQueryHandler
    : IRequestHandler<GetMyRolesQuery, Result<IReadOnlyList<RoleDetailDto>>>
{
    private readonly ICurrentUser     _currentUser;
    private readonly IUserRepository  _users;

    public GetMyRolesQueryHandler(ICurrentUser currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users       = users;
    }

    public async Task<Result<IReadOnlyList<RoleDetailDto>>> Handle(
        GetMyRolesQuery   request,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdWithRolePermissionsAsync(
            _currentUser.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<IReadOnlyList<RoleDetailDto>>(
                "Current user not found.");

        var roles = user.Roles
            .Where(ur => !ur.IsDeleted && ur.Role is not null)
            .Select(ur =>
            {
                var permissions = ur.Role!.Permissions
                    .Where(rp => !rp.IsDeleted && rp.Permission is not null)
                    .Select(rp => new PermissionDto(
                        rp.PermissionId,
                        rp.Permission!.Key,
                        rp.Permission.Description))
                    .OrderBy(p => p.Key)
                    .ToList()
                    .AsReadOnly() as IReadOnlyList<PermissionDto>;

                return new RoleDetailDto(
                    ur.Role.Id,
                    ur.Role.Name,
                    ur.Role.Description,
                    ur.Role.IsSystemRole,
                    permissions);
            })
            .OrderBy(r => r.Name)
            .ToList()
            .AsReadOnly() as IReadOnlyList<RoleDetailDto>;

        return Result.Success(roles);
    }
}
