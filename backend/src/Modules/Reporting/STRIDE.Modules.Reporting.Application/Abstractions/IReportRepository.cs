using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Application.Abstractions;

/// <summary>
/// Repository for persisting <see cref="Report"/> audit records.
/// Implementations are tenant-scoped via <c>TenantAwareRepository</c> in Infrastructure.
/// </summary>
public interface IReportRepository
{
    /// <summary>Returns a report by primary key within the current tenant, or null if not found.</summary>
    Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Stages a new report for insertion. Caller must invoke <see cref="SaveChangesAsync"/>.</summary>
    Task AddAsync(Report report, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
