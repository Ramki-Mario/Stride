using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Queries.GetRole;

internal sealed class GetRoleQueryHandler
    : IRequestHandler<GetRoleQuery, Result<RoleDetailDto>>
{
    private readonly IRoleRepository              _roles;
    private readonly ILogger<GetRoleQueryHandler> _logger;

    public GetRoleQueryHandler(IRoleRepository roles, ILogger<GetRoleQueryHandler> logger)
    {
        _roles  = roles;
        _logger = logger;
    }

    public async Task<Result<RoleDetailDto>> Handle(
        GetRoleQuery      request,
        CancellationToken cancellationToken)
    {
        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            _logger.LogDebug("GetRole: role {RoleId} not found", request.RoleId);
            return Result.Failure<RoleDetailDto>($"Role '{request.RoleId}' not found.");
        }

        var permissions = role.Permissions
            .Where(rp => !rp.IsDeleted)
            .Select(rp => new PermissionDto(rp.PermissionId, rp.Permission!.Key, rp.Permission.Description))
            .OrderBy(p => p.Key)
            .ToList()
            .AsReadOnly() as IReadOnlyList<PermissionDto>;

        return Result.Success(new RoleDetailDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            permissions));
    }
}
