namespace STRIDE.Modules.Workflows.Domain.Enums;

/// <summary>Lifecycle status of an individual StepInstance.</summary>
public enum StepStatus
{
    /// <summary>Step has been created but not yet assigned.</summary>
    Pending = 0,

    /// <summary>Step has been assigned to a user but work has not started.</summary>
    Assigned = 1,

    /// <summary>Assignee has started working on the step.</summary>
    InProgress = 2,

    /// <summary>Step finished successfully.</summary>
    Completed = 3,

    /// <summary>Step was intentionally bypassed.</summary>
    Skipped = 4,

    /// <summary>Step could not be completed due to an error or blocker.</summary>
    Failed = 5,

    /// <summary>Approval-gate step is waiting for an authorised user to approve or reject.</summary>
    AwaitingApproval = 6,

    /// <summary>Approval was rejected by the authorised reviewer.</summary>
    Rejected = 7,
}
