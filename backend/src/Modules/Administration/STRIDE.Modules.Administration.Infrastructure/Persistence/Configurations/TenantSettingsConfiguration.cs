using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence.Configurations;

public sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("TenantSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();
        builder.HasIndex(s => s.TenantId).IsUnique();   // 1:1 per tenant

        builder.Property(s => s.DisplayName)
               .IsRequired(false)
               .HasMaxLength(200)
               .HasDefaultValue(string.Empty);

        builder.Property(s => s.DefaultPalette)
               .IsRequired()
               .HasMaxLength(50)
               .HasDefaultValue("purple");

        builder.Property(s => s.Timezone)
               .IsRequired()
               .HasMaxLength(100)
               .HasDefaultValue("UTC");

        builder.Property(s => s.CustomCssTokensJson)
               .HasColumnType("nvarchar(max)");

        builder.Property(s => s.OnboardingCompleted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).IsRequired();
        builder.Property(s => s.EnabledModules)
               .HasMaxLength(1000)
               .HasColumnType("nvarchar(1000)")
               .HasDefaultValue(null);

        builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
