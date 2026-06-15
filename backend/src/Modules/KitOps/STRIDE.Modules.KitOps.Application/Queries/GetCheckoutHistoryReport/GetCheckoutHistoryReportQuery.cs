using MediatR;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetCheckoutHistoryReport;

public sealed record GetCheckoutHistoryReportQuery(
    Guid      TenantId,
    DateTime? From,
    DateTime? To,
    Guid?     KitItemId) : IRequest<IReadOnlyList<KitCheckoutReportRowDto>>;
