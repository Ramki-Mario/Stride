using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitItems;

internal sealed class GetKitItemsQueryHandler
    : IRequestHandler<GetKitItemsQuery, Result<PagedResult<KitItemSummaryDto>>>
{
    private readonly IKitItemReadService _read;

    public GetKitItemsQueryHandler(IKitItemReadService read) => _read = read;

    public async Task<Result<PagedResult<KitItemSummaryDto>>> Handle(
        GetKitItemsQuery request, CancellationToken cancellationToken)
    {
        var result = await _read.GetKitItemsAsync(
            request.TenantId, request.Search, request.Category, request.IsActive,
            request.Page, request.PageSize, cancellationToken);

        return Result<PagedResult<KitItemSummaryDto>>.Success(result);
    }
}
