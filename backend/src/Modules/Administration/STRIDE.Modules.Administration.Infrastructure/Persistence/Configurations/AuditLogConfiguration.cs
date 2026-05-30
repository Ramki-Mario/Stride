using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.ActorId).IsRequired();
        builder.Property(a => a.ActorEmail).HasMaxLength(256).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ResourceType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ResourceId);
        builder.Property(a => a.OldValueJson);
        builder.Property(a => a.NewValueJson);
        builder.Property(a => a.Timestamp).IsRequired();

        // Primary query pattern: list entries for a tenant ordered by newest first.
        builder.HasIndex(a => new { a.TenantId, a.Timestamp });
    }
}
