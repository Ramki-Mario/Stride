namespace STRIDE.Modules.Workflows.Domain.Enums;

public enum StepType
{
    /// <summary>A standard work step completed by a user.</summary>
    Standard = 0,

    /// <summary>A gate step that requires an authorised approver to approve or reject before the workflow continues.</summary>
    Approval = 1,
}
