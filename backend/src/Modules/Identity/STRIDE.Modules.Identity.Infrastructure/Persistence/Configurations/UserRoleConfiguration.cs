using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.UserId).IsRequired();
        builder.Property(ur => ur.RoleId).IsRequired();
        builder.Property(ur => ur.TenantId).IsRequired();
        builder.Property(ur => ur.CreatedAt).IsRequired();
        builder.Property(ur => ur.UpdatedAt).IsRequired();
        builder.Property(ur => ur.CreatedBy).IsRequired();
        builder.Property(ur => ur.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasOne(ur => ur.Role)
               .WithMany()
               .HasForeignKey(ur => ur.RoleId)
               .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        builder.HasIndex(ur => new { ur.TenantId, ur.UserId, ur.RoleId }).IsUnique();
        builder.HasIndex(ur => new { ur.TenantId, ur.IsDeleted });

        builder.ToTable("UserRoles");
    }
}
