namespace STRIDE.Modules.Invoicing.Application.DTOs;

public sealed record InvoiceLineItemDto(
    Guid    Id,
    string  Description,
    decimal UnitPrice,
    int     Quantity,
    decimal Subtotal);

public sealed record InvoiceDetailDto(
    Guid     Id,
    string   InvoiceNumber,
    string   ClientName,
    string?  ClientEmail,
    string   Currency,
    int      Status,
    string   StatusLabel,
    DateOnly DueDate,
    string?  Notes,
    decimal  TotalAmount,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? PaidAt,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    Guid?    SourceWorkflowInstanceId = null);
