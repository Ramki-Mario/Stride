using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Enums;

namespace STRIDE.Modules.Invoicing.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.ClientName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.ClientEmail).IsRequired().HasMaxLength(320);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(3);
        builder.Property(i => i.Status).IsRequired().HasDefaultValue(InvoiceStatus.Draft);
        builder.Property(i => i.DueDate).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.ClientId);   // nullable FK to clients.Clients (cross-module, no EF nav)
        builder.Property(i => i.TenantId).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).IsRequired();
        builder.Property(i => i.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.Ignore(i => i.TotalAmount); // computed in domain from line items

        // LineItems is exposed as IReadOnlyList; tell EF to use the _lineItems backing field
        builder.HasMany(i => i.LineItems)
               .WithOne()
               .HasForeignKey(l => l.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.LineItems)
               .HasField("_lineItems")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unique invoice number per tenant
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
        builder.HasIndex(i => new { i.TenantId, i.IsDeleted });
        builder.HasIndex(i => new { i.TenantId, i.Status });
        builder.HasIndex(i => new { i.TenantId, i.CreatedAt });
        builder.HasIndex(i => new { i.TenantId, i.ClientId });   // for client history queries
    }
}
