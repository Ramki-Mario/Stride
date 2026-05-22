namespace STRIDE.Modules.Invoicing.API.DTOs;

public sealed record GenerateInvoiceLineItemRequest(
    string  Description,
    decimal UnitPrice,
    int     Quantity);

public sealed record GenerateInvoiceRequest(
    string   InvoiceNumber,
    string   ClientName,
    string   ClientEmail,
    string   Currency,
    DateOnly DueDate,
    string?  Notes,
    IReadOnlyList<GenerateInvoiceLineItemRequest> LineItems);
