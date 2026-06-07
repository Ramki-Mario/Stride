using Microsoft.Extensions.Logging;

namespace STRIDE.Modules.Workflows.Application;

internal static partial class Log
{
    // ── ActivateWorkflow ──────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowDefinition {Id} activated by {UserId}")]
    internal static partial void WorkflowDefinitionActivated(this ILogger logger, Guid id, Guid userId);

    // ── AssignStep ────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Step {StepId} in instance {InstanceId} assigned to {AssigneeId} by {AssignedBy}")]
    internal static partial void StepAssigned(
        this ILogger logger, Guid stepId, Guid instanceId, Guid assigneeId, Guid assignedBy);

    // ── CancelWorkflow ────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowInstance {Id} cancelled by {UserId}")]
    internal static partial void WorkflowInstanceCancelled(this ILogger logger, Guid id, Guid userId);

    // ── CompleteStep ──────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Step {StepId} in instance {InstanceId} completed by {UserId}")]
    internal static partial void StepCompleted(this ILogger logger, Guid stepId, Guid instanceId, Guid userId);

    // ── CreateWorkflow ────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "CreateWorkflow failed: name '{Name}' already exists in tenant {TenantId}")]
    internal static partial void CreateWorkflowDuplicateName(this ILogger logger, string name, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowDefinition {Id} '{Name}' created by {UserId} in tenant {TenantId}")]
    internal static partial void WorkflowDefinitionCreated(
        this ILogger logger, Guid id, string name, Guid userId, Guid tenantId);

    // ── DeleteWorkflow ────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowDefinition {Id} soft-deleted by {UserId}")]
    internal static partial void WorkflowDefinitionDeleted(this ILogger logger, Guid id, Guid userId);

    // ── PauseWorkflow ─────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowInstance {Id} paused by {UserId}")]
    internal static partial void WorkflowInstancePaused(this ILogger logger, Guid id, Guid userId);

    // ── ResumeWorkflow ────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowInstance {Id} resumed by {UserId}")]
    internal static partial void WorkflowInstanceResumed(this ILogger logger, Guid id, Guid userId);

    // ── SkipStep ──────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Step {StepId} in instance {InstanceId} skipped by {UserId}")]
    internal static partial void StepSkipped(this ILogger logger, Guid stepId, Guid instanceId, Guid userId);

    // ── StartWorkflow ─────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowInstance {InstanceId} started from definition {DefinitionId} by {UserId}")]
    internal static partial void WorkflowInstanceStarted(
        this ILogger logger, Guid instanceId, Guid definitionId, Guid userId);

    // ── UpdateWorkflow ────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information,
        Message = "WorkflowDefinition {Id} updated by {UserId}")]
    internal static partial void WorkflowDefinitionUpdated(this ILogger logger, Guid id, Guid userId);
}
