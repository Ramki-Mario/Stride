using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class StepDefinitionConfiguration : IEntityTypeConfiguration<StepDefinition>
{
    public void Configure(EntityTypeBuilder<StepDefinition> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkflowDefinitionId).IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Order).IsRequired();
        builder.Property(s => s.IsRequired).IsRequired();

        // Cross-module FK to Identity.Role — bare nullable Guid, no EF navigation.
        builder.Property(s => s.RequiredRoleId);
        builder.HasIndex(s => s.RequiredRoleId);

        builder.HasIndex(s => new { s.WorkflowDefinitionId, s.Order });

        builder.ToTable("StepDefinitions");
    }
}
