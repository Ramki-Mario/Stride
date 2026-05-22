using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Infrastructure.ReadModels;

/// <summary>
/// Dapper-based implementation of <see cref="IReportingReadService"/>.
/// All SQL is loaded from embedded .sql files — no inline queries in C#.
/// </summary>
internal sealed class ReportingReadService : IReportingReadService
{
    private static readonly string SqlGetDashboardKpis =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetDashboardKpis.sql");

    private static readonly string SqlGetWorkflowTrends =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetWorkflowTrends.sql");

    private static readonly string SqlGetReportSummaries =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetReportSummaries.sql");

    private static readonly string SqlGetWorkflowSummary =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetWorkflowSummary.sql");

    private readonly IDbConnectionFactory _db;

    public ReportingReadService(IDbConnectionFactory db) => _db = db;

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var result = await conn.QuerySingleOrDefaultAsync<DashboardKpiDto>(
            new CommandDefinition(
                SqlGetDashboardKpis,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: ct));

        return result ?? new DashboardKpiDto(0, 0, 0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<WorkflowTrendDto>> GetWorkflowTrendsAsync(
        Guid tenantId,
        int days = 30,
        CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<WorkflowTrendDto>(
            new CommandDefinition(
                SqlGetWorkflowTrends,
                new { TenantId = tenantId, Days = days },
                commandTimeout: 30,
                cancellationToken: ct));

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ReportSummaryDto>> GetReportSummariesAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<ReportSummaryDto>(
            new CommandDefinition(
                SqlGetReportSummaries,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: ct));

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkflowSummaryReportRowDto>> GetWorkflowSummaryAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<WorkflowSummaryReportRowDto>(
            new CommandDefinition(
                SqlGetWorkflowSummary,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: ct));

        return results.ToList().AsReadOnly();
    }
}
