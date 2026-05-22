using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Application.DTOs;

namespace STRIDE.Modules.Invoicing.Application.Queries.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(
    Guid TenantId,
    Guid InvoiceId) : IRequest<Result<InvoiceDetailDto>>;
