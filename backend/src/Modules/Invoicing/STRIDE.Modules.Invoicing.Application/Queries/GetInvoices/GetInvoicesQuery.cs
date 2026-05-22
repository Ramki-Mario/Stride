using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Application.DTOs;

namespace STRIDE.Modules.Invoicing.Application.Queries.GetInvoices;

public sealed record GetInvoicesQuery(
    Guid    TenantId,
    string? Search,
    int?    Status,
    int     Page,
    int     PageSize) : IRequest<Result<PagedResult<InvoiceSummaryDto>>>;
