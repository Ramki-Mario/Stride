using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Application.Abstractions;
using STRIDE.Modules.Invoicing.Application.DTOs;

namespace STRIDE.Modules.Invoicing.Application.Queries.GetInvoices;

internal sealed class GetInvoicesQueryHandler
    : IRequestHandler<GetInvoicesQuery, Result<PagedResult<InvoiceSummaryDto>>>
{
    private readonly IInvoiceReadService _read;

    public GetInvoicesQueryHandler(IInvoiceReadService read) => _read = read;

    public async Task<Result<PagedResult<InvoiceSummaryDto>>> Handle(
        GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var result = await _read.GetInvoicesAsync(
            request.TenantId, request.Search, request.Status,
            request.Page, request.PageSize, cancellationToken);

        return Result<PagedResult<InvoiceSummaryDto>>.Success(result);
    }
}
