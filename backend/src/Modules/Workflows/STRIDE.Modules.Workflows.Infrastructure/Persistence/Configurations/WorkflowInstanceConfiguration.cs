using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.WorkflowDefinitionId).IsRequired();

        builder.Property(i => i.WorkflowName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.StartedBy).IsRequired();
        builder.Property(i => i.ClientId);   // nullable FK to clients.Clients (cross-module, no EF nav)
        builder.Property(i => i.CompletedAt);

        builder.Property(i => i.DeadlineAt)
            .HasColumnType("datetime2");

        builder.Property(i => i.IsSlaBreached)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.SlaBreachedNotifiedAt)
            .HasColumnType("datetime2");

        builder.Property(i => i.TenantId).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).IsRequired();
        builder.Property(i => i.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(i => new { i.TenantId, i.IsDeleted });
        builder.HasIndex(i => new { i.TenantId, i.Status });
        builder.HasIndex(i => new { i.TenantId, i.WorkflowDefinitionId });
        builder.HasIndex(i => new { i.TenantId, i.CreatedAt });
        builder.HasIndex(i => new { i.TenantId, i.ClientId });    // for client history queries

        // Steps is exposed as IReadOnlyList; tell EF Core to use the _steps backing field.
        builder.HasMany(i => i.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Steps)
            .HasField("_steps")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ToTable("WorkflowInstances");
    }
}
