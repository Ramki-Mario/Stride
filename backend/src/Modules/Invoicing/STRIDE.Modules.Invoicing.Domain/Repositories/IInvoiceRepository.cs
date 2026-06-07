using STRIDE.Modules.Invoicing.Domain.Entities;

namespace STRIDE.Modules.Invoicing.Domain.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByNumberAsync(Guid tenantId, string invoiceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the draft invoice automatically created for a completed workflow instance,
    /// or null if none exists. Used to enforce idempotency on the auto-draft creation.
    /// </summary>
    Task<Invoice?> GetBySourceWorkflowInstanceIdAsync(Guid tenantId, Guid workflowInstanceId, CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
