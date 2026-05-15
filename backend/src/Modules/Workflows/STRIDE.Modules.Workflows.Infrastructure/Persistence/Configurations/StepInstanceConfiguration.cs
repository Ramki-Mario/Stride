using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class StepInstanceConfiguration : IEntityTypeConfiguration<StepInstance>
{
    public void Configure(EntityTypeBuilder<StepInstance> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkflowInstanceId).IsRequired();
        builder.Property(s => s.StepDefinitionId).IsRequired();

        builder.Property(s => s.StepName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Order).IsRequired();
        builder.Property(s => s.IsRequired).IsRequired();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.AssigneeId);

        builder.Property(s => s.FailureReason)
            .HasMaxLength(2000);

        builder.Property(s => s.CompletedAt);

        builder.HasIndex(s => new { s.WorkflowInstanceId, s.Order });
        builder.HasIndex(s => new { s.WorkflowInstanceId, s.Status });
        builder.HasIndex(s => s.AssigneeId);

        builder.ToTable("StepInstances");
    }
}
