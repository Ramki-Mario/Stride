using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Abstractions;

public interface IKitUserHistoryReadService
{
    Task<IReadOnlyList<MyKitCheckoutDto>> GetMyCheckoutsAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<MyKitReservationDto>> GetMyReservationsAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<KitCatalogItemDto>> GetCatalogWithAvailabilityAsync(
        Guid tenantId, CancellationToken ct = default);
}
