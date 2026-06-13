using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Webhooks.Domain.Entities;

namespace STRIDE.Modules.Webhooks.Infrastructure.Persistence.Configurations;

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.WebhookSubscriptionId).IsRequired();
        builder.Property(d => d.EventType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Payload).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(d => d.Url).IsRequired().HasMaxLength(2048);
        builder.Property(d => d.Status).IsRequired();
        builder.Property(d => d.ResponseCode);
        builder.Property(d => d.ResponseBody).HasMaxLength(1000);
        builder.Property(d => d.AttemptCount).IsRequired().HasDefaultValue(0);
        builder.Property(d => d.NextAttemptAt);
        builder.Property(d => d.LastAttemptAt);

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).IsRequired();
        builder.Property(d => d.IsDeleted).IsRequired().HasDefaultValue(false);

        // Primary lookup: retry scanner queries this index.
        builder.HasIndex(d => new { d.Status, d.NextAttemptAt });
        // Tenant-scoped delivery log queries.
        builder.HasIndex(d => new { d.TenantId, d.WebhookSubscriptionId, d.CreatedAt });

        // No EF navigation to WebhookSubscription — bare FK column, no nav property
        // (subscriptions and deliveries are managed through separate repositories).
    }
}
