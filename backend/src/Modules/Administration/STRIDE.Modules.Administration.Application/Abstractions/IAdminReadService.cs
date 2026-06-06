using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Queries.GetUsers;

namespace STRIDE.Modules.Administration.Application.Abstractions;

public interface IAdminReadService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(
        Guid tenantId,
        string? search,
        string? role,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
