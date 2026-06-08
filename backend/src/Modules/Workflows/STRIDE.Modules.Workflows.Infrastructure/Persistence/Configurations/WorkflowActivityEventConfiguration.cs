using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowActivityEventConfiguration
    : IEntityTypeConfiguration<WorkflowActivityEvent>
{
    public void Configure(EntityTypeBuilder<WorkflowActivityEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.WorkflowInstanceId).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ActorUserId).IsRequired();

        builder.Property(e => e.Payload)
            .HasMaxLength(2000);

        builder.Property(e => e.OccurredAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Activity events are append-only — no soft-delete, no UpdatedAt column.

        // Primary query: all events for a given instance, tenant-scoped, ordered by time.
        builder.HasIndex(e => new { e.WorkflowInstanceId, e.TenantId, e.OccurredAt })
            .HasDatabaseName("IX_WorkflowActivityEvents_Instance_Tenant_Time");

        builder.HasOne<WorkflowInstance>()
            .WithMany()
            .HasForeignKey(e => e.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("WorkflowActivityEvents");
    }
}
