using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Infrastructure.Persistence.Configurations;

internal sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.UserId).IsRequired();

        builder.Property(s => s.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(s => s.P256dh)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(s => s.Auth)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(s => s.CreatedAt).IsRequired();

        // Unique index: one row per endpoint across the tenant
        builder.HasIndex(s => new { s.TenantId, s.Endpoint }).IsUnique();

        // Fast lookup: all subscriptions for a user within a tenant
        builder.HasIndex(s => new { s.TenantId, s.UserId });

        builder.ToTable("PushSubscriptions");
    }
}
