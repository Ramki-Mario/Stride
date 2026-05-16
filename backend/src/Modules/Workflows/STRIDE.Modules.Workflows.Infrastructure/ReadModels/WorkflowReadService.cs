using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Infrastructure.ReadModels;

/// <summary>
/// Dapper-based implementation of <see cref="IWorkflowReadService"/>.
/// Reads directly from the <c>workflows</c> SQL schema using raw SQL —
/// faster and simpler than EF Core for list/dashboard projections.
/// </summary>
internal sealed class WorkflowReadService : IWorkflowReadService
{
    private static readonly string SqlGetDefinitionSummaries =
        SqlLoader.Load(typeof(WorkflowReadService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetWorkflowDefinitionSummaries.sql");

    private static readonly string SqlGetInstanceSummaries =
        SqlLoader.Load(typeof(WorkflowReadService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetWorkflowInstanceSummaries.sql");

    private static readonly string SqlGetInstanceSummariesByDefinition =
        SqlLoader.Load(typeof(WorkflowReadService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetWorkflowInstanceSummariesByDefinition.sql");

    private static readonly string SqlGetDashboardStats =
        SqlLoader.Load(typeof(WorkflowReadService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetDashboardStats.sql");

    private readonly string _connectionString;

    public WorkflowReadService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured.");
    }

    public async Task<IReadOnlyList<WorkflowDefinitionReadModel>> GetWorkflowDefinitionSummariesAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var results = await conn.QueryAsync<WorkflowDefinitionReadModel>(
            SqlGetDefinitionSummaries,
            new { TenantId = tenantId },
            commandTimeout: 30);

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkflowInstanceReadModel>> GetWorkflowInstanceSummariesAsync(
        Guid tenantId,
        Guid? definitionId = null,
        CancellationToken ct = default)
    {
        var sql = definitionId.HasValue
            ? SqlGetInstanceSummariesByDefinition
            : SqlGetInstanceSummaries;

        await using var conn = new SqlConnection(_connectionString);
        var results = await conn.QueryAsync<WorkflowInstanceReadModel>(
            sql,
            new { TenantId = tenantId, DefinitionId = definitionId },
            commandTimeout: 30);

        return results.ToList().AsReadOnly();
    }

    public async Task<WorkflowDashboardStats> GetDashboardStatsAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var stats = await conn.QuerySingleOrDefaultAsync<WorkflowDashboardStats>(
            SqlGetDashboardStats,
            new { TenantId = tenantId },
            commandTimeout: 30);

        return stats ?? new WorkflowDashboardStats(0, 0, 0, 0, 0, 0);
    }
}
