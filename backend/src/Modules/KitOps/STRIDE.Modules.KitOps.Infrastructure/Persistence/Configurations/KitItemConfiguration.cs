using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence.Configurations;

internal sealed class KitItemConfiguration : IEntityTypeConfiguration<KitItem>
{
    public void Configure(EntityTypeBuilder<KitItem> builder)
    {
        builder.ToTable("KitItems");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name)
            .IsRequired()
            .HasMaxLength(KitItem.NameMaxLength);

        builder.Property(k => k.Category)
            .IsRequired()
            .HasMaxLength(KitItem.CategoryMaxLength);

        builder.Property(k => k.Description)
            .HasMaxLength(KitItem.DescriptionMaxLength);

        builder.Property(k => k.TotalQuantity)
            .IsRequired();

        builder.Property(k => k.IsActive)
            .IsRequired();

        // Unique name per tenant (among non-deleted items only)
        builder.HasIndex(k => new { k.TenantId, k.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(k => new { k.TenantId, k.IsDeleted });
        builder.HasIndex(k => new { k.TenantId, k.IsActive });
        builder.HasIndex(k => new { k.TenantId, k.Category });

        builder.Ignore(k => k.DomainEvents);
    }
}
