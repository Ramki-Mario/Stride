using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Scheduling.Domain.Entities;
using STRIDE.Modules.Scheduling.Domain.ValueObjects;

namespace STRIDE.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class ScheduleDefinitionConfiguration
    : IEntityTypeConfiguration<ScheduleDefinition>
{
    public void Configure(EntityTypeBuilder<ScheduleDefinition> builder)
    {
        builder.ToTable("ScheduleDefinitions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(ScheduleDefinition.NameMaxLength);

        builder.Property(s => s.Description)
            .HasMaxLength(ScheduleDefinition.DescriptionMaxLength);

        builder.Property(s => s.WorkflowDefinitionId)
            .IsRequired();

        builder.Property(s => s.CronExpression)
            .IsRequired()
            .HasMaxLength(100)
            .HasConversion(
                v => v.Value,
                s => CronExpression.Create(s));

        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.NextRunAt);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.CreatedBy).IsRequired();
        builder.Property(s => s.IsDeleted).IsRequired();

        builder.HasIndex(s => new { s.TenantId, s.IsDeleted });
        builder.HasIndex(s => new { s.TenantId, s.IsActive });
    }
}
