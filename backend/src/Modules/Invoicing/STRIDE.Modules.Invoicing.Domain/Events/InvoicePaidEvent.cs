using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Invoicing.Domain.Events;

public sealed record InvoicePaidEvent(
    Guid     InvoiceId,
    Guid     TenantId,
    string   InvoiceNumber,
    decimal  TotalAmount,
    string   Currency,
    Guid     MarkedBy) : IDomainEvent;
