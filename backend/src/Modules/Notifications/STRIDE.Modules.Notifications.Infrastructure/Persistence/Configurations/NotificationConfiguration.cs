using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientId).IsRequired();

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Body)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.TenantId).IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.UpdatedAt).IsRequired();
        builder.Property(n => n.CreatedBy).IsRequired();
        builder.Property(n => n.IsDeleted).IsRequired().HasDefaultValue(false);

        // Core query patterns: by recipient + tenant, unread filter, recency
        builder.HasIndex(n => new { n.TenantId, n.RecipientId, n.IsDeleted });
        builder.HasIndex(n => new { n.TenantId, n.RecipientId, n.IsRead, n.IsDeleted });
        builder.HasIndex(n => new { n.TenantId, n.CreatedAt });

        builder.ToTable("Notifications");
    }
}
