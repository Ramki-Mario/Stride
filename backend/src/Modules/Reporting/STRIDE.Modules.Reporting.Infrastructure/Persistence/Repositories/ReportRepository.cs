using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReportRepository"/>.
/// Inherits tenant + soft-delete filtering from <see cref="TenantAwareRepository{TEntity,TContext}"/>.
/// </summary>
internal sealed class ReportRepository
    : TenantAwareRepository<Report, ReportingDbContext>, IReportRepository
{
    public ReportRepository(ReportingDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Query.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(Report report, CancellationToken ct = default)
        => await Context.Set<Report>().AddAsync(report, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
