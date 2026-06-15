using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Abstractions;

public interface IKitItemReadService
{
    Task<PagedResult<KitItemSummaryDto>> GetKitItemsAsync(
        Guid tenantId, string? search, string? category, bool? isActive,
        int page, int pageSize, CancellationToken ct = default);

    Task<KitItemDetailDto?> GetKitItemByIdAsync(
        Guid tenantId, Guid kitItemId, CancellationToken ct = default);
}
