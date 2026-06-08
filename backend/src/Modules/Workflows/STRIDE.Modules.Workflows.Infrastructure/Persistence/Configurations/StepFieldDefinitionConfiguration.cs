using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Configurations;

internal sealed class StepFieldDefinitionConfiguration : IEntityTypeConfiguration<StepFieldDefinition>
{
    public void Configure(EntityTypeBuilder<StepFieldDefinition> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.StepDefinitionId).IsRequired();

        builder.Property(f => f.Label)
            .IsRequired()
            .HasMaxLength(200);

        // FieldType stored as its string name for readability in the DB.
        builder.Property(f => f.FieldType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(f => f.IsRequired).IsRequired();
        builder.Property(f => f.DisplayOrder).IsRequired();

        builder.Property(f => f.HelpText)
            .HasMaxLength(500);

        // DropdownOptionsJson is internal — mapped via shadow-property access.
        // EF Core maps private/internal properties when using the backing-field convention.
        builder.Property(f => f.DropdownOptionsJson)
            .HasColumnName("DropdownOptionsJson")
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(f => new { f.StepDefinitionId, f.DisplayOrder });

        builder.ToTable("StepFieldDefinitions");
    }
}
