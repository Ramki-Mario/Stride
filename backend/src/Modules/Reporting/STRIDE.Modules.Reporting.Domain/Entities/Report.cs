using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Reporting.Domain.Entities;

/// <summary>
/// Audit record for a generated report.  Captures the who/what/when of report generation
/// without storing the full data payload — the payload is re-derived on export.
/// </summary>
public sealed class Report : AuditableEntity
{
    // Required by EF Core
    private Report() { }

    /// <summary>Human-readable label for the report (e.g. "KPI Snapshot — 2026-05-19").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Identifies the type of data this report contains.</summary>
    public ReportType ReportType { get; private set; }

    /// <summary>UTC timestamp when the report was generated.</summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>Number of data rows produced (1 for KPI snapshots, N for trend/summary reports).</summary>
    public int RecordCount { get; private set; }

    /// <summary>
    /// Factory method — the only valid way to create a <see cref="Report"/>.
    /// Enforces all invariants at construction time.
    /// </summary>
    public static Report Create(
        Guid tenantId,
        Guid createdBy,
        string name,
        ReportType reportType,
        int recordCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (recordCount < 0)
            throw new ArgumentOutOfRangeException(nameof(recordCount), "RecordCount cannot be negative.");

        var now = DateTime.UtcNow;

        return new Report
        {
            Id          = Guid.NewGuid(),
            TenantId    = tenantId,
            CreatedBy   = createdBy,
            Name        = name,
            ReportType  = reportType,
            GeneratedAt = now,
            RecordCount = recordCount,
            CreatedAt   = now,
            UpdatedAt   = now,
            IsDeleted   = false
        };
    }
}
