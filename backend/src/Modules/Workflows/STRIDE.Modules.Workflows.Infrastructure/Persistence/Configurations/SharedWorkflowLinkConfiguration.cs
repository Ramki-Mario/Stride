using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class SharedWorkflowLinkConfiguration : IEntityTypeConfiguration<SharedWorkflowLink>
{
    public void Configure(EntityTypeBuilder<SharedWorkflowLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.WorkflowInstanceId).IsRequired();

        builder.Property(l => l.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(l => l.ExpiresAt).IsRequired().HasColumnType("datetime2");
        builder.Property(l => l.RevokedAt).HasColumnType("datetime2");
        builder.Property(l => l.ViewCount).IsRequired().HasDefaultValue(0);

        builder.Property(l => l.TenantId).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();
        builder.Property(l => l.CreatedBy).IsRequired();
        builder.Property(l => l.IsDeleted).IsRequired().HasDefaultValue(false);

        // Token lookup is the hot path (public endpoint resolves by token) and must be unique.
        builder.HasIndex(l => l.Token).IsUnique();

        // Management UI lists active links for one instance, tenant-scoped.
        builder.HasIndex(l => new { l.WorkflowInstanceId, l.TenantId });

        builder.ToTable("SharedWorkflowLinks");
    }
}
