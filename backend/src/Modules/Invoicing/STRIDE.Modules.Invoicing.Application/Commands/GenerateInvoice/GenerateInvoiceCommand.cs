using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Invoicing.Application.Commands.GenerateInvoice;

public sealed record GenerateInvoiceLineItem(
    string  Description,
    decimal UnitPrice,
    int     Quantity);

public sealed record GenerateInvoiceCommand(
    Guid   TenantId,
    string InvoiceNumber,
    string ClientName,
    string ClientEmail,
    string Currency,
    DateOnly DueDate,
    string?  Notes,
    IReadOnlyList<GenerateInvoiceLineItem> LineItems,
    Guid   CreatedBy,
    Guid?  ClientId = null) : IRequest<Result<Guid>>;
