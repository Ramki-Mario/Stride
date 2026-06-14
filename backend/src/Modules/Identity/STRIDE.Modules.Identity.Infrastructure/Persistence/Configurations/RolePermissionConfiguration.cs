using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.RoleId).IsRequired();
        builder.Property(rp => rp.PermissionId).IsRequired();
        builder.Property(rp => rp.TenantId).IsRequired();
        builder.Property(rp => rp.CreatedAt).IsRequired();
        builder.Property(rp => rp.UpdatedAt).IsRequired();
        builder.Property(rp => rp.CreatedBy).IsRequired();
        builder.Property(rp => rp.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(rp => new { rp.TenantId, rp.RoleId, rp.PermissionId }).IsUnique();
        builder.HasIndex(rp => new { rp.TenantId, rp.IsDeleted });

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("RolePermissions");
    }
}
