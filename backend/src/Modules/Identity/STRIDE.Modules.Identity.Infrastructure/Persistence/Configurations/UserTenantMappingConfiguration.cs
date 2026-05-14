using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserTenantMappingConfiguration : IEntityTypeConfiguration<UserTenantMapping>
{
    public void Configure(EntityTypeBuilder<UserTenantMapping> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.RoleName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();
        builder.Property(m => m.CreatedBy).IsRequired();
        builder.Property(m => m.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
        builder.HasIndex(m => new { m.TenantId, m.IsDeleted });

        builder.ToTable("UserTenantMappings");
    }
}
