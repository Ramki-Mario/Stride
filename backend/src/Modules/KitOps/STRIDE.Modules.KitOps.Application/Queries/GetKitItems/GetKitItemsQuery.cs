using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitItems;

public sealed record GetKitItemsQuery(
    Guid    TenantId,
    string? Search,
    string? Category,
    bool?   IsActive,
    int     Page     = 1,
    int     PageSize = 20) : IRequest<Result<PagedResult<KitItemSummaryDto>>>;
