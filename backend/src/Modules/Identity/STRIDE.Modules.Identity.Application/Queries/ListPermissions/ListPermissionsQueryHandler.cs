using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Queries.GetRole;

namespace STRIDE.Modules.Identity.Application.Queries.ListPermissions;

internal sealed class ListPermissionsQueryHandler
    : IRequestHandler<ListPermissionsQuery, Result<IReadOnlyList<PermissionDto>>>
{
    private readonly IPermissionRepository _permissions;

    public ListPermissionsQueryHandler(IPermissionRepository permissions)
        => _permissions = permissions;

    public async Task<Result<IReadOnlyList<PermissionDto>>> Handle(
        ListPermissionsQuery  request,
        CancellationToken     cancellationToken)
    {
        var all = await _permissions.GetAllAsync(cancellationToken);

        var dtos = all
            .Select(p => new PermissionDto(p.Id, p.Key, p.Description))
            .ToList()
            .AsReadOnly() as IReadOnlyList<PermissionDto>;

        return Result.Success(dtos);
    }
}
