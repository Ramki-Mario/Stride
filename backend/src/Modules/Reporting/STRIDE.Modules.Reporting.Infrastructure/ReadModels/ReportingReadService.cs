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

    private static readonly string SqlGetOverdueAlerts =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetOverdueAlerts.sql");

    private static readonly string SqlGetUnassignedStepAlerts =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetUnassignedStepAlerts.sql");

    private static readonly string SqlGetSlaAtRiskAlerts =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetSlaAtRiskAlerts.sql");

    private static readonly string SqlGetReadyToInvoiceAlerts =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetReadyToInvoiceAlerts.sql");

    private static readonly string SqlGetTeamWorkloadSummary =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetTeamWorkloadSummary.sql");

    private static readonly string SqlGetTeamWorkloadSteps =
        SqlLoader.Load(typeof(ReportingReadService).Assembly,
            "STRIDE.Modules.Reporting.Infrastructure.ReadModels.Queries.GetTeamWorkloadSteps.sql");

    private readonly IDbConnectionFactory _db;

    public ReportingReadService(IDbConnectionFactory db) => _db = db;

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var result = await conn.QuerySingleOrDefaultAsync<DashboardKpiDto>(
            new CommandDefinition(
                SqlGetDashboardKpis,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return result ?? new DashboardKpiDto(0, 0, 0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<WorkflowTrendDto>> GetWorkflowTrendsAsync(
        Guid tenantId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<WorkflowTrendDto>(
            new CommandDefinition(
                SqlGetWorkflowTrends,
                new { TenantId = tenantId, Days = days },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ReportSummaryDto>> GetReportSummariesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<ReportSummaryDto>(
            new CommandDefinition(
                SqlGetReportSummaries,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkflowSummaryReportRowDto>> GetWorkflowSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<WorkflowSummaryReportRowDto>(
            new CommandDefinition(
                SqlGetWorkflowSummary,
                new { TenantId = tenantId },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

        return results.ToList().AsReadOnly();
    }

    public async Task<DashboardAlertSummaryDto> GetDashboardAlertsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var param = new { TenantId = tenantId };

        // Each query gets its own pooled connection — no MARS required.
        var overdueTask    = QueryListAsync<OverdueAlertItemDto>(SqlGetOverdueAlerts,        param, cancellationToken);
        var unassignedTask = QueryListAsync<UnassignedStepAlertItemDto>(SqlGetUnassignedStepAlerts, param, cancellationToken);
        var slaTask        = QueryListAsync<SlaAtRiskAlertItemDto>(SqlGetSlaAtRiskAlerts,    param, cancellationToken);
        var invoiceTask    = QueryListAsync<ReadyToInvoiceAlertItemDto>(SqlGetReadyToInvoiceAlerts, param, cancellationToken);

        await Task.WhenAll(overdueTask, unassignedTask, slaTask, invoiceTask);

        return new DashboardAlertSummaryDto(
            Overdue:         await overdueTask,
            UnassignedSteps: await unassignedTask,
            SlaAtRisk:       await slaTask,
            ReadyToInvoice:  await invoiceTask);
    }

    public async Task<IReadOnlyList<TeamWorkloadItemDto>> GetTeamWorkloadAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var param = new { TenantId = tenantId };

        var summaryTask = QueryListAsync<TeamWorkloadSummaryRow>(SqlGetTeamWorkloadSummary, param, cancellationToken);
        var stepsTask   = QueryListAsync<TeamMemberStepRow>(SqlGetTeamWorkloadSteps, param, cancellationToken);

        await Task.WhenAll(summaryTask, stepsTask);

        var stepsByUser = (await stepsTask)
            .GroupBy(s => s.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TeamMemberStepDto>)g
                    .Select(s => new TeamMemberStepDto(
                        s.StepId, s.StepName, s.WorkflowInstanceId, s.WorkflowName, s.DueAt, s.IsOverdue))
                    .ToList()
                    .AsReadOnly());

        return (await summaryTask)
            .Select(u => new TeamWorkloadItemDto(
                UserId:          u.UserId,
                DisplayName:     u.DisplayName,
                Email:           u.Email,
                ActiveStepCount: u.ActiveStepCount,
                HasOverdueSteps: u.HasOverdueSteps,
                TopSteps:        stepsByUser.TryGetValue(u.UserId, out var steps)
                                 ? steps
                                 : Array.Empty<TeamMemberStepDto>()))
            .ToList()
            .AsReadOnly();
    }

    private async Task<IReadOnlyList<T>> QueryListAsync<T>(
        string sql, object param, CancellationToken cancellationToken)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);
        var results = await conn.QueryAsync<T>(
            new CommandDefinition(sql, param, commandTimeout: 30, cancellationToken: cancellationToken));
        return results.ToList().AsReadOnly();
    }

    // ── Private row types (Dapper projection targets) ─────────────────────────

    private sealed record TeamWorkloadSummaryRow(
        Guid   UserId,
        string DisplayName,
        string Email,
        int    ActiveStepCount,
        bool   HasOverdueSteps);

    private sealed record TeamMemberStepRow(
        Guid      UserId,
        Guid      StepId,
        string    StepName,
        Guid      WorkflowInstanceId,
        string    WorkflowName,
        DateTime? DueAt,
        bool      IsOverdue);
}
