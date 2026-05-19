using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
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

    private readonly string _connectionString;

    public ReportingReadService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var result = await conn.QuerySingleOrDefaultAsync<DashboardKpiDto>(
            SqlGetDashboardKpis,
            new { TenantId = tenantId },
            commandTimeout: 30);

        return result ?? new DashboardKpiDto(0, 0, 0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<WorkflowTrendDto>> GetWorkflowTrendsAsync(
        Guid tenantId,
        int days = 30,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var results = await conn.QueryAsync<WorkflowTrendDto>(
            SqlGetWorkflowTrends,
            new { TenantId = tenantId, Days = days },
            commandTimeout: 30);

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ReportSummaryDto>> GetReportSummariesAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var results = await conn.QueryAsync<ReportSummaryDto>(
            SqlGetReportSummaries,
            new { TenantId = tenantId },
            commandTimeout: 30);

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkflowSummaryReportRowDto>> GetWorkflowSummaryAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var results = await conn.QueryAsync<WorkflowSummaryReportRowDto>(
            SqlGetWorkflowSummary,
            new { TenantId = tenantId },
            commandTimeout: 30);

        return results.ToList().AsReadOnly();
    }
}
