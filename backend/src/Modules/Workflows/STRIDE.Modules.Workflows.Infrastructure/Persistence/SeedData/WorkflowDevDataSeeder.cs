using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Infrastructure.Persistence;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.SeedData;

/// <summary>
/// Development-only workflow seeder.
///
/// Seeds realistic workflow definitions and instances so the Dashboard KPI
/// cards and trend charts render with meaningful data immediately after cloning.
///
/// What gets created:
///   Definitions  — 3 Active (Employee Onboarding, Invoice Approval, IT Access Request)
///                  1 Draft  (Leave Request)
///                  1 Archived (Supplier Registration)
///
///   Instances    — ~20 instances spread across the 3 active definitions:
///                  Running(5), Completed(10), Failed(2), Cancelled(1)
///
///   Timestamps   — CreatedAt is backdated via raw SQL after insert so the
///                  trend chart shows a 30-day time series, not a single spike.
///
/// Idempotent — checks for existing definitions by name before inserting.
/// Skipped entirely outside the Development environment.
///
/// Called from STRIDE.Host Program.cs:
///   await WorkflowDevDataSeeder.SeedAsync(app, tenantId, userId);
/// </summary>
public static class WorkflowDevDataSeeder
{
    private static readonly Guid SystemActorId = new("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IHost host, Guid tenantId, Guid userId)
    {
        var env = host.Services.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment()) return;

        await using var scope = host.Services.CreateAsyncScope();
        var db     = scope.ServiceProvider.GetRequiredService<WorkflowsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WorkflowsDbContext>>();

        // ── Skip entirely if workflow definitions already exist ───────────────
        var anyExist = await db.WorkflowDefinitions
            .IgnoreQueryFilters()
            .AnyAsync(d => d.TenantId == tenantId && !d.IsDeleted);

        if (anyExist)
        {
            logger.LogInformation("[WorkflowSeed] Workflow data already present — skipping.");
            return;
        }

        logger.LogInformation("[WorkflowSeed] Seeding workflow definitions and instances...");

        // ── 1. Definitions ────────────────────────────────────────────────────

        var onboarding = BuildDefinition(tenantId, "Employee Onboarding",
            "End-to-end new-hire process from offer acceptance to first day.",
            new[]
            {
                ("Send Offer Letter",          "Draft and send the formal offer letter to the candidate.",    true),
                ("Background Check",           "Initiate and track the background verification check.",       true),
                ("IT Equipment Provisioning",  "Order and configure laptop, accounts, and access cards.",     true),
                ("Induction Session Scheduled","Schedule first-day induction with HR and line manager.",      false),
            });
        onboarding.Activate(userId);

        var invoiceApproval = BuildDefinition(tenantId, "Invoice Approval",
            "Three-stage approval chain for supplier invoices.",
            new[]
            {
                ("Finance Review",   "Finance team validates invoice details and PO matching.",   true),
                ("Manager Approval", "Departmental manager reviews and approves the spend.",      true),
                ("Payment Scheduled","Finance schedules payment run and notifies supplier.",      true),
            });
        invoiceApproval.Activate(userId);

        var itAccess = BuildDefinition(tenantId, "IT Access Request",
            "Self-service request for system access and permissions.",
            new[]
            {
                ("Request Submitted", "Requester submits access form with justification.",     true),
                ("Manager Sign-off",  "Line manager approves the access request.",             true),
                ("IT Provisioning",   "IT grants access and confirms via email to requester.", true),
            });
        itAccess.Activate(userId);

        var leaveRequest = BuildDefinition(tenantId, "Leave Request",
            "Annual and sick-leave approval workflow (draft — pending HR policy sign-off).",
            new[]
            {
                ("Submit Request",      "Employee fills in leave dates and type.", true),
                ("Manager Approval",    "Line manager approves or rejects.",       true),
            });
        // Leave Request stays Draft — no Activate() call.

        var supplierReg = BuildDefinition(tenantId, "Supplier Registration",
            "Legacy supplier onboarding — replaced by Supplier Portal in Q3.",
            new[]
            {
                ("Supplier Form",  "Supplier completes registration form.", true),
                ("Procurement Review", "Procurement validates and approves supplier.", true),
            });
        supplierReg.Activate(userId);
        supplierReg.Archive();

        ClearEvents(onboarding, invoiceApproval, itAccess, leaveRequest, supplierReg);

        db.WorkflowDefinitions.AddRange(onboarding, invoiceApproval, itAccess, leaveRequest, supplierReg);
        await db.SaveChangesAsync();

        // ── 2. Instances ──────────────────────────────────────────────────────
        // Build a list of (instance, daysAgo) tuples.
        // daysAgo drives the SQL backdate so the trend chart spans 30 days.

        var instances = new List<(WorkflowInstance Instance, int DaysAgo)>();

        // Employee Onboarding — 5 completed, 2 running, 1 failed
        instances.AddRange(BuildCompleted(onboarding, userId, count: 5, daysStart: 28));
        instances.AddRange(BuildRunning(onboarding,   userId, count: 2));
        instances.Add(BuildFailed(onboarding, userId, daysAgo: 10,
                                  failStepIndex: 1, reason: "Background check returned adverse findings."));

        // Invoice Approval — 4 completed, 2 running, 1 cancelled
        instances.AddRange(BuildCompleted(invoiceApproval, userId, count: 4, daysStart: 25));
        instances.AddRange(BuildRunning(invoiceApproval,   userId, count: 2));
        instances.Add(BuildCancelled(invoiceApproval, userId, daysAgo: 5));

        // IT Access Request — 1 completed, 1 running, 1 failed
        instances.AddRange(BuildCompleted(itAccess, userId, count: 1, daysStart: 3));
        instances.AddRange(BuildRunning(itAccess,   userId, count: 1));
        instances.Add(BuildFailed(itAccess, userId, daysAgo: 7,
                                  failStepIndex: 0, reason: "Request lacked mandatory justification."));

        // Clear domain events on all instances before saving
        foreach (var (inst, _) in instances)
            inst.ClearDomainEvents();

        db.WorkflowInstances.AddRange(instances.Select(t => t.Instance));
        await db.SaveChangesAsync();

        // ── 3. Backdate CreatedAt via raw SQL ─────────────────────────────────
        // Each instance gets a CreatedAt = UTC now minus its daysAgo value so the
        // Dapper trend query aggregates by day over the past 30 days.
        foreach (var (inst, daysAgo) in instances)
        {
            if (daysAgo <= 0) continue;

            var offset = -daysAgo;
            var id     = inst.Id;
            await db.Database.ExecuteSqlAsync(
                $"""
                UPDATE workflows.WorkflowInstances
                SET    CreatedAt = DATEADD(DAY, {offset}, GETUTCDATE()),
                       UpdatedAt = DATEADD(DAY, {offset}, GETUTCDATE())
                WHERE  Id = {id}
                """);
        }

        logger.LogInformation("[WorkflowSeed] ✓ Workflow seed complete — {Count} instances across 3 active definitions.",
            instances.Count);
    }

    // ── Build helpers ─────────────────────────────────────────────────────────

    private static WorkflowDefinition BuildDefinition(
        Guid tenantId,
        string name,
        string description,
        IEnumerable<(string Name, string Desc, bool Required)> steps)
    {
        var def = WorkflowDefinition.Create(tenantId, name, description, SystemActorId);
        foreach (var (stepName, stepDesc, required) in steps)
            def.AddStep(stepName, stepDesc, required);
        return def;
    }

    /// <summary>Returns <paramref name="count"/> completed instances. daysStart is the
    /// oldest daysAgo; each subsequent instance is 2–4 days closer to today.</summary>
    private static IEnumerable<(WorkflowInstance, int DaysAgo)> BuildCompleted(
        WorkflowDefinition def, Guid userId, int count, int daysStart)
    {
        var step = daysStart;
        for (var i = 0; i < count; i++)
        {
            var inst = WorkflowInstance.Start(def, userId);
            foreach (var s in inst.Steps.ToList())
                inst.CompleteStep(s.Id, userId);

            yield return (inst, step);
            step = Math.Max(1, step - Random.Shared.Next(2, 5));
        }
    }

    private static IEnumerable<(WorkflowInstance, int DaysAgo)> BuildRunning(
        WorkflowDefinition def, Guid userId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var inst = WorkflowInstance.Start(def, userId);
            yield return (inst, 0); // today
        }
    }

    private static (WorkflowInstance, int DaysAgo) BuildFailed(
        WorkflowDefinition def, Guid userId, int daysAgo, int failStepIndex, string reason)
    {
        var inst = WorkflowInstance.Start(def, userId);
        var stepToFail = inst.Steps[Math.Min(failStepIndex, inst.Steps.Count - 1)];
        inst.FailStep(stepToFail.Id, reason, userId);
        return (inst, daysAgo);
    }

    private static (WorkflowInstance, int DaysAgo) BuildCancelled(
        WorkflowDefinition def, Guid userId, int daysAgo)
    {
        var inst = WorkflowInstance.Start(def, userId);
        inst.Cancel(userId);
        return (inst, daysAgo);
    }

    private static void ClearEvents(params WorkflowDefinition[] defs)
    {
        foreach (var d in defs) d.ClearDomainEvents();
    }
}
