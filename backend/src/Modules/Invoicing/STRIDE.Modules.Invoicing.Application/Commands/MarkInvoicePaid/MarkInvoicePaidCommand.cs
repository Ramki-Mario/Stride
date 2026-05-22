using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Invoicing.Application.Commands.MarkInvoicePaid;

public sealed record MarkInvoicePaidCommand(
    Guid TenantId,
    Guid InvoiceId,
    Guid MarkedBy) : IRequest<Result>;
