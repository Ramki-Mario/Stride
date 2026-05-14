using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class TenantDomainMappingConfiguration : IEntityTypeConfiguration<TenantDomainMapping>
{
    public void Configure(EntityTypeBuilder<TenantDomainMapping> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.CorporateDomain).IsRequired().HasMaxLength(253);
        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).IsRequired();
        builder.Property(d => d.IsDeleted).IsRequired().HasDefaultValue(false);

        // Unique active domain — one domain maps to exactly one tenant
        builder.HasIndex(d => new { d.CorporateDomain, d.IsDeleted }).IsUnique();
        builder.HasIndex(d => new { d.TenantId, d.IsDeleted });

        builder.ToTable("TenantDomainMappings");
    }
}
