using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Plan).IsRequired().HasMaxLength(50);
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
        builder.Property(t => t.CreatedBy).IsRequired();
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => new { t.IsDeleted, t.IsActive });

        builder.HasMany(t => t.DomainMappings)
            .WithOne()
            .HasForeignKey(dm => dm.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Tenants");
    }
}
