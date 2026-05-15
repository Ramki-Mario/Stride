using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Infrastructure.ReadModels;

/// <summary>
/// Dapper-based implementation of <see cref="IWorkflowReadService"/>.
/// Reads directly from the <c>workflows</c> SQL schema using raw SQL —
/// faster and simpler than EF Core for list/dashboard projections.
/// </summary>
internal sealed class WorkflowReadService : IWorkflowReadService
{
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
        const string sql = """
            SELECT
                d.Id,
                d.Name,
                d.Description,
                d.Status,
                COUNT(s.Id) AS StepCount,
                d.CreatedAt,
                d.UpdatedAt
            FROM workflows.WorkflowDefinitions d
            LEFT JOIN workflows.StepDefinitions s ON s.WorkflowDefinitionId = d.Id
            WHERE d.TenantId = @TenantId
              AND d.IsDeleted = 0
            GROUP BY d.Id, d.Name, d.Description, d.Status, d.CreatedAt, d.UpdatedAt
            ORDER BY d.UpdatedAt DESC
            """;

        await using var conn = new SqlConnection(_connectionString);
        var results = await conn.QueryAsync<WorkflowDefinitionReadModel>(
            sql,
            new { TenantId = tenantId },
            commandTimeout: 30);

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkflowInstanceReadModel>> GetWorkflowInstanceSummariesAsync(
        Guid tenantId,
        Guid? definitionId = null,
        CancellationToken ct = default)
    {
        const string baseSql = """
            SELECT
                i.Id,
                i.WorkflowDefinitionId,
                i.WorkflowName,
                i.Status,
                COUNT(s.Id)                                        AS TotalSteps,
                SUM(CASE WHEN s.Status IN ('Completed','Skipped') THEN 1 ELSE 0 END) AS CompletedSteps,
                i.CreatedAt,
                i.CompletedAt
            FROM workflows.WorkflowInstances i
            LEFT JOIN workflows.StepInstances s ON s.WorkflowInstanceId = i.Id
            WHERE i.TenantId = @TenantId
              AND i.IsDeleted = 0
            """;

        var sql = definitionId.HasValue
            ? baseSql + " AND i.WorkflowDefinitionId = @DefinitionId GROUP BY i.Id, i.WorkflowDefinitionId, i.WorkflowName, i.Status, i.CreatedAt, i.CompletedAt ORDER BY i.CreatedAt DESC"
            : baseSql + " GROUP BY i.Id, i.WorkflowDefinitionId, i.WorkflowName, i.Status, i.CreatedAt, i.CompletedAt ORDER BY i.CreatedAt DESC";

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
        const string sql = """
            SELECT
                COUNT(*)                                                          AS TotalDefinitions,
                SUM(CASE WHEN d.Status = 'Active'    THEN 1 ELSE 0 END)          AS ActiveDefinitions,
                (SELECT COUNT(*) FROM workflows.WorkflowInstances i
                 WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
                   AND i.Status = 'Running')                                      AS TotalRunning,
                (SELECT COUNT(*) FROM workflows.WorkflowInstances i
                 WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
                   AND i.Status = 'Completed')                                    AS TotalCompleted,
                (SELECT COUNT(*) FROM workflows.WorkflowInstances i
                 WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
                   AND i.Status = 'Failed')                                       AS TotalFailed,
                (SELECT COUNT(*) FROM workflows.WorkflowInstances i
                 WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
                   AND i.Status = 'Cancelled')                                    AS TotalCancelled
            FROM workflows.WorkflowDefinitions d
            WHERE d.TenantId = @TenantId
              AND d.IsDeleted = 0
            """;

        await using var conn = new SqlConnection(_connectionString);
        var stats = await conn.QuerySingleOrDefaultAsync<WorkflowDashboardStats>(
            sql,
            new { TenantId = tenantId },
            commandTimeout: 30);

        return stats ?? new WorkflowDashboardStats(0, 0, 0, 0, 0, 0);
    }
}
