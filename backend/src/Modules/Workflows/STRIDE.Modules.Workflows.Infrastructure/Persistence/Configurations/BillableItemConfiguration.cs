using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class BillableItemConfiguration : IEntityTypeConfiguration<BillableItem>
{
    public void Configure(EntityTypeBuilder<BillableItem> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.StepInstanceId).IsRequired();
        builder.Property(b => b.TenantId).IsRequired();

        builder.Property(b => b.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.Quantity)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(b => b.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(b => b.Unit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.CreatedAt).IsRequired();

        // Ignore computed property — not stored
        builder.Ignore(b => b.LineTotal);

        builder.HasIndex(b => b.StepInstanceId);
        builder.HasIndex(b => new { b.TenantId, b.StepInstanceId });

        builder.ToTable("BillableItems");
    }
}
