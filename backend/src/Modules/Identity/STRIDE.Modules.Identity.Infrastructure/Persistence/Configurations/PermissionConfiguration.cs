using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Resource).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Action).IsRequired().HasMaxLength(50);
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).IsRequired();
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(p => new { p.TenantId, p.Name }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.IsDeleted });

        builder.ToTable("Permissions");
    }
}
