using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Webhooks.Domain.Entities;

namespace STRIDE.Modules.Webhooks.Infrastructure.Persistence.Configurations;

public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Url).IsRequired().HasMaxLength(2048);

        // Stored encrypted (base64 of IV ‖ ciphertext) — generous length for AES output.
        builder.Property(s => s.SigningSecret).IsRequired().HasMaxLength(512);

        builder.Property(s => s.EventTypesJson).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).IsRequired();
        builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(s => new { s.TenantId, s.IsDeleted });
        builder.HasIndex(s => new { s.TenantId, s.IsActive });
    }
}
