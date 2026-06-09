using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.WorkflowInstanceId).IsRequired();
        builder.Property(a => a.StepInstanceId).IsRequired();
        builder.Property(a => a.TenantId).IsRequired();

        // Cross-module FK to Identity.Role — bare nullable Guid, no EF navigation.
        builder.Property(a => a.RequestedFromRoleId);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.RejectionHandling)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RejectionHandling.HaltWorkflow);

        builder.Property(a => a.RevertToStepOrder);

        builder.Property(a => a.DecisionByUserId);

        builder.Property(a => a.DecisionAt)
            .HasColumnType("datetime2");

        builder.Property(a => a.Comment)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.HasIndex(a => new { a.TenantId, a.WorkflowInstanceId });
        builder.HasIndex(a => new { a.StepInstanceId, a.Status });

        builder.ToTable("ApprovalRequests");
    }
}
