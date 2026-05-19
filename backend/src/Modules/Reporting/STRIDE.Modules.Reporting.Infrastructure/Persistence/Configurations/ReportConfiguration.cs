using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.CreatedBy).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.ReportType)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();  // stored as "DashboardKpi" | "WorkflowTrend" | "WorkflowSummary"

        builder.Property(r => r.GeneratedAt).IsRequired();
        builder.Property(r => r.RecordCount).IsRequired();

        // Tenant-scoped list queries
        builder.HasIndex(r => new { r.TenantId, r.IsDeleted, r.GeneratedAt })
            .HasDatabaseName("IX_Reports_TenantId_GeneratedAt");
    }
}
