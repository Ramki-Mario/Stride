using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Queries.GetUser;

internal sealed class GetUserQueryHandler
    : IRequestHandler<GetUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ILogger<GetUserQueryHandler> _logger;

    public GetUserQueryHandler(
        IUserRepository users,
        IRoleRepository roles,
        ILogger<GetUserQueryHandler> logger)
    {
        _users  = users;
        _roles  = roles;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetUserQuery request, CancellationToken ct)
    {
        // ITenantContext is already set by TenantMiddleware for authenticated requests.

        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning("GetUser: user {UserId} not found", request.UserId);
            return Result.Failure<UserDto>($"User '{request.UserId}' not found.");
        }

        var allRoles  = await _roles.GetAllAsync(ct);
        var roleIndex = allRoles.ToDictionary(r => r.Id, r => r.Name);
        var roleNames = user.Roles
            .Where(ur => !ur.IsDeleted && roleIndex.ContainsKey(ur.RoleId))
            .Select(ur => roleIndex[ur.RoleId])
            .ToList()
            .AsReadOnly();

        return Result.Success(new UserDto(
            Id:          user.Id,
            TenantId:    user.TenantId,
            Email:       user.Email,
            DisplayName: user.DisplayName,
            IsActive:    user.IsActive,
            Roles:       roleNames));
    }
}
