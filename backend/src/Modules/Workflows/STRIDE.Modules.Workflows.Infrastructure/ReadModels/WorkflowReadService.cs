using Dapper;
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

    private static readonly string SqlGetInstanceSummariesByTeam =
        SqlLoader.Load(typeof(WorkflowReadService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetWorkflowInstanceSummariesByTeam.sql");

    private static readonly string SqlGetDashboardStats =
        SqlLoader.Load(typeof(WorkflowReadService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetDashboardStats.sql");

    private static readonly string SqlGetMyTasks =
        SqlLoader.Load(typeof(WorkflowReadService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetMyTasks.sql");

    private readonly IDbConnectionFactory _db;

    public WorkflowReadService(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<WorkflowDefinitionReadModel>> GetWorkflowDefinitionSummariesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<WorkflowDefinitionReadModel>(
            new CommandDefinition(
                SqlGetDefinitionSummaries,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkflowInstanceReadModel>> GetWorkflowInstanceSummariesAsync(
        Guid tenantId,
        Guid? definitionId = null,
        Guid? teamId = null,
        CancellationToken cancellationToken = default)
    {
        string sql;
        object param;

        if (definitionId.HasValue)
        {
            sql   = SqlGetInstanceSummariesByDefinition;
            param = new { TenantId = tenantId, DefinitionId = definitionId };
        }
        else if (teamId.HasValue)
        {
            sql   = SqlGetInstanceSummariesByTeam;
            param = new { TenantId = tenantId, TeamId = teamId };
        }
        else
        {
            sql   = SqlGetInstanceSummaries;
            param = new { TenantId = tenantId };
        }

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<WorkflowInstanceReadModel>(
            new CommandDefinition(
                sql,
                param,
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return results.ToList().AsReadOnly();
    }

    public async Task<WorkflowDashboardStats> GetDashboardStatsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var stats = await conn.QuerySingleOrDefaultAsync<WorkflowDashboardStats>(
            new CommandDefinition(
                SqlGetDashboardStats,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return stats ?? new WorkflowDashboardStats(0, 0, 0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<MyTaskReadModel>> GetMyTasksAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<MyTaskReadModel>(
            new CommandDefinition(
                SqlGetMyTasks,
                new { UserId = userId, TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return results.ToList().AsReadOnly();
    }
}
