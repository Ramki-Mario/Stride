using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Queries.ListRoles;

internal sealed class ListRolesQueryHandler
    : IRequestHandler<ListRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    private readonly IRoleRepository _roles;
    private readonly ILogger<ListRolesQueryHandler> _logger;

    public ListRolesQueryHandler(IRoleRepository roles, ILogger<ListRolesQueryHandler> logger)
    {
        _roles  = roles;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(
        ListRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _roles.GetAllAsync(cancellationToken);

        var dtos = roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description))
            .ToList()
            .AsReadOnly() as IReadOnlyList<RoleDto>;

        _logger.LogDebug("ListRoles: returned {Count} role(s)", dtos.Count);
        return Result.Success(dtos);
    }
}
