using MediatR;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitUsageSummary;

internal sealed class GetKitUsageSummaryQueryHandler
    : IRequestHandler<GetKitUsageSummaryQuery, IReadOnlyList<KitUsageSummaryRowDto>>
{
    private readonly IKitUsageReportService _reports;

    public GetKitUsageSummaryQueryHandler(IKitUsageReportService reports)
        => _reports = reports;

    public Task<IReadOnlyList<KitUsageSummaryRowDto>> Handle(
        GetKitUsageSummaryQuery request, CancellationToken cancellationToken)
        => _reports.GetUsageSummaryAsync(
            request.TenantId,
            request.From,
            request.To,
            cancellationToken);
}
