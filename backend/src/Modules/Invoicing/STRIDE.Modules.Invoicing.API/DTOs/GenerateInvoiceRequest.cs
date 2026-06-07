using System.Text.Json.Serialization;

namespace STRIDE.Modules.Invoicing.API.DTOs;

public sealed record GenerateInvoiceLineItemRequest(
    string                        Description,
    [property: JsonRequired] decimal UnitPrice,
    [property: JsonRequired] int     Quantity);

public sealed record GenerateInvoiceRequest(
    string                        InvoiceNumber,
    string                        ClientName,
    string                        ClientEmail,
    string                        Currency,
    [property: JsonRequired] DateOnly DueDate,
    string?                       Notes,
    IReadOnlyList<GenerateInvoiceLineItemRequest> LineItems,
    Guid?                         ClientId = null);  // optional link to a Clients record
