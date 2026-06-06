using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Infrastructure.Persistence;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly InvoicingDbContext _ctx;

    public InvoiceRepository(InvoicingDbContext ctx) => _ctx = ctx;

    public Task<Invoice?> GetByIdAsync(Guid tenantId, Guid invoiceId, CancellationToken cancellationToken = default) =>
        _ctx.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == invoiceId, cancellationToken);

    public Task<Invoice?> GetByNumberAsync(Guid tenantId, string invoiceNumber, CancellationToken cancellationToken = default) =>
        _ctx.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(
                i => i.TenantId == tenantId && i.InvoiceNumber == invoiceNumber, cancellationToken);

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
        await _ctx.Invoices.AddAsync(invoice, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ctx.SaveChangesAsync(cancellationToken);
}
