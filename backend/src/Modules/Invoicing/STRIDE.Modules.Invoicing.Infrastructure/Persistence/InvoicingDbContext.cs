using Microsoft.EntityFrameworkCore;

namespace STRIDE.Modules.Invoicing.Infrastructure.Persistence;

public sealed class InvoicingDbContext : DbContext
{
    public InvoicingDbContext(DbContextOptions<InvoicingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("invoicing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
