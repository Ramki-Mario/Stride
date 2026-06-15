using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Abstractions;

public interface IKitUsageReportService
{
    Task<IReadOnlyList<KitCheckoutReportRowDto>> GetCheckoutHistoryAsync(
        Guid      tenantId,
        DateTime? from,
        DateTime? to,
        Guid?     kitItemId,
        CancellationToken ct = default);

    Task<IReadOnlyList<KitUsageSummaryRowDto>> GetUsageSummaryAsync(
        Guid      tenantId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
}
