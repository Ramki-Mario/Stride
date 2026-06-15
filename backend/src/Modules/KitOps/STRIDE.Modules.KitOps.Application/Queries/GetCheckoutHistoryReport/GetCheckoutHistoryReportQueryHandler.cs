using MediatR;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetCheckoutHistoryReport;

internal sealed class GetCheckoutHistoryReportQueryHandler
    : IRequestHandler<GetCheckoutHistoryReportQuery, IReadOnlyList<KitCheckoutReportRowDto>>
{
    private readonly IKitUsageReportService _reports;

    public GetCheckoutHistoryReportQueryHandler(IKitUsageReportService reports)
        => _reports = reports;

    public Task<IReadOnlyList<KitCheckoutReportRowDto>> Handle(
        GetCheckoutHistoryReportQuery request, CancellationToken cancellationToken)
        => _reports.GetCheckoutHistoryAsync(
            request.TenantId,
            request.From,
            request.To,
            request.KitItemId,
            cancellationToken);
}
