using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.WorkflowInstanceId).IsRequired();
        builder.Property(a => a.StepInstanceId);   // nullable — null means instance-level attachment

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.StorageKey)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.FileSizeBytes).IsRequired();

        // CreatedBy holds the uploading user's ID (see Attachment.Create).
        builder.Property(a => a.CreatedBy).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);

        // Enforce referential integrity to WorkflowInstances.
        // Cascade-delete: removing an instance removes all its attachment rows
        // (physical files should be cleaned up separately via IFileStorageService.DeleteAsync).
        builder.HasOne<WorkflowInstance>()
            .WithMany()
            .HasForeignKey(a => a.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Step-level attachments reference a specific step, but deleting a step
        // should not cascade-delete the attachment (the evidence still belongs to the job).
        builder.HasOne<StepInstance>()
            .WithMany()
            .HasForeignKey(a => a.StepInstanceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Query indexes
        builder.HasIndex(a => new { a.TenantId, a.WorkflowInstanceId, a.IsDeleted })
            .HasDatabaseName("IX_Attachments_TenantId_WorkflowInstanceId");

        builder.HasIndex(a => new { a.TenantId, a.StepInstanceId, a.IsDeleted })
            .HasDatabaseName("IX_Attachments_TenantId_StepInstanceId");

        builder.HasIndex(a => a.StorageKey)
            .HasDatabaseName("IX_Attachments_StorageKey");

        builder.ToTable("Attachments");
    }
}
