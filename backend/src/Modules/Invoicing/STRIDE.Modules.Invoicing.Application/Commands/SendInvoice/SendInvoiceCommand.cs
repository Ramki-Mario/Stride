using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Invoicing.Application.Commands.SendInvoice;

public sealed record SendInvoiceCommand(
    Guid TenantId,
    Guid InvoiceId,
    Guid SentBy) : IRequest<Result>;
