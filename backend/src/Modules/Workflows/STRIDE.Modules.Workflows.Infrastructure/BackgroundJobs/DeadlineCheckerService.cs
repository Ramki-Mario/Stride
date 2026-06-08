using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Infrastructure.Persistence;

namespace STRIDE.Modules.Workflows.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically scans for:
/// <list type="bullet">
///   <item>Step instances whose <c>DueAt</c> has passed but have not yet been flagged overdue.</item>
///   <item>Workflow instances whose SLA <c>DeadlineAt</c> has passed but have not yet been flagged breached.</item>
/// </list>
/// For each match the corresponding aggregate method is called, which raises a domain event.
/// <see cref="WorkflowsDbContext.SaveChangesAsync"/> then dispatches those events so the
/// Notifications module can create in-app alerts.
///
/// This service intentionally bypasses the tenant-scoped repositories because it must operate
/// across all tenants; <see cref="WorkflowsDbContext"/> is resolved directly from the scope.
/// </summary>
internal sealed class DeadlineCheckerService : BackgroundService
{
    // Check every 15 minutes.
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadlineCheckerService> _logger;

    public DeadlineCheckerService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeadlineCheckerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DeadlineCheckerService started. Interval: {Interval}", Interval);

        using var timer = new PeriodicTimer(Interval);

        // Run once on startup, then on each tick.
        do
        {
            try
            {
                await CheckDeadlinesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeadlineCheckerService encountered an error during deadline scan.");
                // Continue running — transient errors should not kill the service.
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        _logger.LogInformation("DeadlineCheckerService stopped.");
    }

    private async Task CheckDeadlinesAsync(CancellationToken ct)
    {
        await using var scope  = _scopeFactory.CreateAsyncScope();
        var dbContext           = scope.ServiceProvider.GetRequiredService<WorkflowsDbContext>();
        var now                 = DateTime.UtcNow;

        await FlagOverdueStepsAsync(dbContext, now, ct);
        await FlagSlaBreachesAsync(dbContext, now, ct);
    }

    // ── Step overdue detection ─────────────────────────────────────────────────

    private async Task FlagOverdueStepsAsync(WorkflowsDbContext dbContext, DateTime now, CancellationToken ct)
    {
        // Find workflow instance IDs that have at least one overdue, unflagged, non-terminal step.
        var overdueStepRecords = await dbContext.StepInstances
            .Where(s => s.DueAt != null
                     && s.DueAt < now
                     && !s.IsOverdue
                     && s.Status != StepStatus.Completed
                     && s.Status != StepStatus.Skipped
                     && s.Status != StepStatus.Failed)
            .Select(s => new { s.Id, s.WorkflowInstanceId })
            .ToListAsync(ct);

        if (overdueStepRecords.Count == 0) return;

        _logger.LogInformation(
            "DeadlineChecker: {Count} overdue step(s) detected across {WfCount} workflow instance(s).",
            overdueStepRecords.Count,
            overdueStepRecords.Select(s => s.WorkflowInstanceId).Distinct().Count());

        var workflowIds = overdueStepRecords
            .Select(s => s.WorkflowInstanceId)
            .Distinct()
            .ToList();

        // Load the affected workflow instances with their steps so domain methods can be called.
        // We do NOT include BillableItems / FieldValues — they are not needed here.
        var instances = await dbContext.WorkflowInstances
            .Include(i => i.Steps)
            .Where(i => workflowIds.Contains(i.Id)
                     && i.Status == WorkflowStatus.Running
                     && !i.IsDeleted)
            .ToListAsync(ct);

        foreach (var instance in instances)
        {
            var stepIds = overdueStepRecords
                .Where(s => s.WorkflowInstanceId == instance.Id)
                .Select(s => s.Id);

            foreach (var stepId in stepIds)
                instance.MarkStepOverdue(stepId);
        }

        if (instances.Count > 0)
            await dbContext.SaveChangesAsync(ct);
    }

    // ── SLA breach detection ───────────────────────────────────────────────────

    private async Task FlagSlaBreachesAsync(WorkflowsDbContext dbContext, DateTime now, CancellationToken ct)
    {
        // Load running instances whose SLA deadline has passed and have not yet been flagged.
        var breachedInstances = await dbContext.WorkflowInstances
            .Where(i => i.DeadlineAt != null
                     && i.DeadlineAt < now
                     && !i.IsSlaBreached
                     && i.Status == WorkflowStatus.Running
                     && !i.IsDeleted)
            .ToListAsync(ct);

        if (breachedInstances.Count == 0) return;

        _logger.LogInformation(
            "DeadlineChecker: {Count} SLA breach(es) detected.",
            breachedInstances.Count);

        foreach (var instance in breachedInstances)
            instance.MarkSlaBreached();

        await dbContext.SaveChangesAsync(ct);
    }
}
