using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasConversion(
                p => p.Hash,
                h => STRIDE.Modules.Identity.Domain.ValueObjects.Password.FromHash(h))
            .HasMaxLength(512);

        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.TenantId).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).IsRequired();
        builder.Property(u => u.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(u => new { u.TenantId, u.NormalizedEmail }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.CreatedAt });
        builder.HasIndex(u => new { u.TenantId, u.IsDeleted });

        builder.HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Users");
    }
}
