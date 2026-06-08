using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class StepFieldValueConfiguration : IEntityTypeConfiguration<StepFieldValue>
{
    public void Configure(EntityTypeBuilder<StepFieldValue> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.StepInstanceId).IsRequired();
        builder.Property(v => v.StepFieldDefinitionId).IsRequired();
        builder.Property(v => v.TenantId).IsRequired();

        builder.Property(v => v.Value)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(v => v.CreatedAt).IsRequired();

        builder.HasOne<StepFieldDefinition>()
            .WithMany()
            .HasForeignKey(v => v.StepFieldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.StepInstanceId);
        builder.HasIndex(v => new { v.TenantId, v.StepInstanceId });
        builder.HasIndex(v => new { v.TenantId, v.StepInstanceId, v.StepFieldDefinitionId })
            .IsUnique();

        builder.ToTable("StepFieldValues");
    }
}
