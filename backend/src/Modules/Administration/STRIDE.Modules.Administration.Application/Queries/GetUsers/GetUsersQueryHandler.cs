using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Queries.GetUsers;

internal sealed class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, Result<PagedResult<AdminUserDto>>>
{
    private readonly IAdminReadService _readService;

    public GetUsersQueryHandler(IAdminReadService readService)
        => _readService = readService;

    public async Task<Result<PagedResult<AdminUserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readService.GetUsersAsync(
            request.TenantId,
            request.Search,
            request.Role,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(page);
    }
}
