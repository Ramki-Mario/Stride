using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Invoicing.Domain.Entities;

namespace STRIDE.Modules.Invoicing.Infrastructure.Persistence.Configurations;

public sealed class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Property(l => l.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.Currency).IsRequired().HasMaxLength(3);

        builder.Ignore(l => l.Subtotal); // computed in domain
    }
}
