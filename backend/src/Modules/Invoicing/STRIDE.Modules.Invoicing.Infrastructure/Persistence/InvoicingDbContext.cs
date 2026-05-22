using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Invoicing.Domain.Entities;

namespace STRIDE.Modules.Invoicing.Infrastructure.Persistence;

public sealed class InvoicingDbContext : DbContext
{
    public DbSet<Invoice>         Invoices     { get; set; } = null!;
    public DbSet<InvoiceLineItem> LineItems    { get; set; } = null!;

    public InvoicingDbContext(DbContextOptions<InvoicingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("invoicing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
