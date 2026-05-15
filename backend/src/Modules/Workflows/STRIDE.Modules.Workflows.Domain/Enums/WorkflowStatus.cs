namespace STRIDE.Modules.Workflows.Domain.Enums;

/// <summary>
/// Lifecycle status shared by WorkflowDefinition (Draft/Active/Archived)
/// and WorkflowInstance (Running/Paused/Completed/Cancelled/Failed).
/// </summary>
public enum WorkflowStatus
{
    /// <summary>Definition is being configured — not yet runnable.</summary>
    Draft = 0,

    /// <summary>Definition is published and can be started.</summary>
    Active = 1,

    /// <summary>An instance is actively executing.</summary>
    Running = 2,

    /// <summary>An instance has been temporarily suspended.</summary>
    Paused = 3,

    /// <summary>An instance finished successfully (all steps done/skipped).</summary>
    Completed = 4,

    /// <summary>An instance was manually stopped before completion.</summary>
    Cancelled = 5,

    /// <summary>An instance terminated due to a step failure.</summary>
    Failed = 6,

    /// <summary>A definition is retired and can no longer be started.</summary>
    Archived = 7,
}
