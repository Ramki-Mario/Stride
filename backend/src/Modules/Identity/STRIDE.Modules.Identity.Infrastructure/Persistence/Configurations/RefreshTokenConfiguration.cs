using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.Token).IsRequired().HasMaxLength(128);
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.RevokedAt);
        builder.Property(r => r.ReplacedByToken).HasMaxLength(128);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).IsRequired();
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);

        // token value is globally unique (256-bit random)
        builder.HasIndex(r => r.Token).IsUnique();
        // scan all active tokens for a user (revoke-all, login cleanup)
        builder.HasIndex(r => new { r.TenantId, r.UserId });
        // expiry cleanup scans
        builder.HasIndex(r => new { r.TenantId, r.ExpiresAt });

        builder.ToTable("RefreshTokens");
    }
}
