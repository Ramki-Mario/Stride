using MediatR;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitUsageSummary;

public sealed record GetKitUsageSummaryQuery(
    Guid      TenantId,
    DateTime? From,
    DateTime? To) : IRequest<IReadOnlyList<KitUsageSummaryRowDto>>;
