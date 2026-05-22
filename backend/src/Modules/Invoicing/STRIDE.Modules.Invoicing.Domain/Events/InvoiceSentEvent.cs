using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Invoicing.Domain.Events;

public sealed record InvoiceSentEvent(
    Guid   InvoiceId,
    Guid   TenantId,
    string InvoiceNumber,
    Guid   SentBy) : IDomainEvent;
