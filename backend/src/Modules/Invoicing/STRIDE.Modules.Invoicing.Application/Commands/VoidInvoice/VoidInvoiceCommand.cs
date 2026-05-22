using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Invoicing.Application.Commands.VoidInvoice;

public sealed record VoidInvoiceCommand(
    Guid TenantId,
    Guid InvoiceId,
    Guid VoidedBy) : IRequest<Result>;
