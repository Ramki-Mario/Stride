using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Infrastructure.ReadModels;

/// <summary>
/// Dapper-based implementation of <see cref="IAnalyticsReadService"/>.
/// All SQL is loaded from embedded .sql files — no inline queries in C#.
/// </summary>
internal sealed class AnalyticsReadService : IAnalyticsReadService
{
    private static readonly string SqlGetWorkflowCompletionTimes =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetWorkflowCompletionTimes.sql");

    private static readonly string SqlGetStepBottlenecks =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetStepBottlenecks.sql");

    private static readonly string SqlGetCompletionTimeTrend =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetCompletionTimeTrend.sql");

    private static readonly string SqlGetWorkflowDefinitions =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetAnalyticsWorkflowDefinitions.sql");

    private static readonly string SqlGetCompletionTimesExport =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetCompletionTimesExport.sql");

    private static readonly string SqlGetTeamPerformance =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetTeamPerformance.sql");

    private static readonly string SqlGetRevenueMonthlyTrend =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetRevenueMonthlyTrend.sql");

    private static readonly string SqlGetRevenueByWorkflowType =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetRevenueByWorkflowType.sql");

    private static readonly string SqlGetRevenueByClient =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetRevenueByClient.sql");

    private static readonly string SqlGetRevenueExport =
        SqlLoader.Load(typeof(AnalyticsReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetRevenueExport.sql");

    private readonly IDbConnectionFactory _db;

    public AnalyticsReadService(IDbConnectionFactory db) => _db = db;

    public async Task<CompletionTimeAnalyticsDto> GetCompletionTimesAsync(
        Guid      tenantId,
        DateTime  fromDate,
        DateTime  toDate,
        Guid?     workflowDefinitionId = null,
        CancellationToken cancellationToken = default)
    {
        var param = new
        {
            TenantId             = tenantId,
            FromDate             = fromDate,
            ToDate               = toDate,
            WorkflowDefinitionId = workflowDefinitionId,
        };

        var byDefinitionTask = QueryListAsync<WorkflowCompletionTimeDto>(
            SqlGetWorkflowCompletionTimes, param, cancellationToken);

        var bottlenecksTask = QueryListAsync<StepBottleneckDto>(
            SqlGetStepBottlenecks, param, cancellationToken);

        var trendTask = QueryListAsync<CompletionTrendDto>(
            SqlGetCompletionTimeTrend, param, cancellationToken);

        await Task.WhenAll(byDefinitionTask, bottlenecksTask, trendTask);

        return new CompletionTimeAnalyticsDto(
            ByDefinition: await byDefinitionTask,
            Bottlenecks:  await bottlenecksTask,
            WeeklyTrend:  await trendTask);
    }

    public async Task<IReadOnlyList<AnalyticsWorkflowDefinitionDto>> GetWorkflowDefinitionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await QueryListAsync<AnalyticsWorkflowDefinitionDto>(
            SqlGetWorkflowDefinitions,
            new { TenantId = tenantId },
            cancellationToken);
    }

    public async Task<IReadOnlyList<CompletionTimeExportRowDto>> GetCompletionTimesExportAsync(
        Guid      tenantId,
        DateTime  fromDate,
        DateTime  toDate,
        Guid?     workflowDefinitionId = null,
        CancellationToken cancellationToken = default)
    {
        return await QueryListAsync<CompletionTimeExportRowDto>(
            SqlGetCompletionTimesExport,
            new
            {
                TenantId             = tenantId,
                FromDate             = fromDate,
                ToDate               = toDate,
                WorkflowDefinitionId = workflowDefinitionId,
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<TeamMemberPerformanceDto>> GetTeamPerformanceAsync(
        Guid      tenantId,
        DateTime  fromDate,
        DateTime  toDate,
        Guid?     roleId = null,
        CancellationToken cancellationToken = default)
    {
        return await QueryListAsync<TeamMemberPerformanceDto>(
            SqlGetTeamPerformance,
            new
            {
                TenantId = tenantId,
                FromDate = fromDate,
                ToDate   = toDate,
                RoleId   = roleId,
            },
            cancellationToken);
    }

    public async Task<(IReadOnlyList<RevenueMonthlyDto> Monthly,
                       IReadOnlyList<RevenueByWorkflowTypeDto> ByWorkflowType,
                       IReadOnlyList<RevenueByClientDto> ByClient)>
        GetRevenueAsync(
            Guid      tenantId,
            DateTime  fromDate,
            DateTime  toDate,
            CancellationToken cancellationToken = default)
    {
        var param = new { TenantId = tenantId, FromDate = fromDate, ToDate = toDate };

        var monthlyTask      = QueryListAsync<RevenueMonthlyDto>(SqlGetRevenueMonthlyTrend,  param, cancellationToken);
        var byWorkflowTask   = QueryListAsync<RevenueByWorkflowTypeDto>(SqlGetRevenueByWorkflowType, param, cancellationToken);
        var byClientTask     = QueryListAsync<RevenueByClientDto>(SqlGetRevenueByClient,     param, cancellationToken);

        await Task.WhenAll(monthlyTask, byWorkflowTask, byClientTask);

        return (await monthlyTask, await byWorkflowTask, await byClientTask);
    }

    public async Task<IReadOnlyList<RevenueExportRowDto>> GetRevenueExportAsync(
        Guid      tenantId,
        DateTime  fromDate,
        DateTime  toDate,
        CancellationToken cancellationToken = default)
    {
        return await QueryListAsync<RevenueExportRowDto>(
            SqlGetRevenueExport,
            new { TenantId = tenantId, FromDate = fromDate, ToDate = toDate },
            cancellationToken);
    }

    private async Task<IReadOnlyList<T>> QueryListAsync<T>(
        string sql, object param, CancellationToken cancellationToken)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<T>(
            new CommandDefinition(sql, param, commandTimeout: 30, cancellationToken: cancellationToken));
        return results.ToList().AsReadOnly();
    }
}
