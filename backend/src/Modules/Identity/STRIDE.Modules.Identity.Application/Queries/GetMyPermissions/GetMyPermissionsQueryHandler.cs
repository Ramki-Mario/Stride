using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Queries.GetMyPermissions;

internal sealed class GetMyPermissionsQueryHandler
    : IRequestHandler<GetMyPermissionsQuery, Result<IReadOnlyList<string>>>
{
    private readonly ICurrentUser            _currentUser;
    private readonly ITenantContext          _tenantContext;
    private readonly IUserPermissionService  _permissions;

    public GetMyPermissionsQueryHandler(
        ICurrentUser           currentUser,
        ITenantContext         tenantContext,
        IUserPermissionService permissions)
    {
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
        _permissions   = permissions;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(
        GetMyPermissionsQuery request, CancellationToken cancellationToken)
    {
        var keys = await _permissions.GetPermissionsAsync(
            _tenantContext.TenantId,
            _currentUser.UserId,
            cancellationToken);

        return Result.Success<IReadOnlyList<string>>(keys.ToList());
    }
}
