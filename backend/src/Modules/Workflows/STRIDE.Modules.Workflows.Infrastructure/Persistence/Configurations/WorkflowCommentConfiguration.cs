using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowCommentConfiguration : IEntityTypeConfiguration<WorkflowComment>
{
    public void Configure(EntityTypeBuilder<WorkflowComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.WorkflowInstanceId).IsRequired();

        builder.Property(c => c.Body)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(c => c.EditedAt)
            .HasColumnType("datetime2");

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);

        // Primary query path: all comments for a given instance, tenant-scoped.
        builder.HasIndex(c => new { c.WorkflowInstanceId, c.TenantId, c.CreatedAt });
        builder.HasIndex(c => new { c.TenantId, c.IsDeleted });

        builder.ToTable("WorkflowComments");
    }
}
