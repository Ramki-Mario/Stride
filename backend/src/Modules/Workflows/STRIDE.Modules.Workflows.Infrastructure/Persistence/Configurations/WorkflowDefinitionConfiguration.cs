using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).IsRequired();
        builder.Property(d => d.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.Property(d => d.SlaOffsetHours)
            .HasColumnType("decimal(6,2)");

        builder.HasIndex(d => new { d.TenantId, d.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(d => new { d.TenantId, d.IsDeleted });
        builder.HasIndex(d => new { d.TenantId, d.Status });

        // Steps is exposed as IReadOnlyList; tell EF Core to use the _steps backing field.
        builder.HasMany(d => d.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Steps)
            .HasField("_steps")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ToTable("WorkflowDefinitions");
    }
}
