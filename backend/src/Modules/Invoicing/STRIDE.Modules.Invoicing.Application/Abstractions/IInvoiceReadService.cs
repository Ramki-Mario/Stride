using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Application.DTOs;

namespace STRIDE.Modules.Invoicing.Application.Abstractions;

public interface IInvoiceReadService
{
    Task<PagedResult<InvoiceSummaryDto>> GetInvoicesAsync(
        Guid    tenantId,
        string? search,
        int?    status,
        int     page,
        int     pageSize,
        CancellationToken cancellationToken = default);

    Task<InvoiceDetailDto?> GetInvoiceByIdAsync(
        Guid tenantId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
