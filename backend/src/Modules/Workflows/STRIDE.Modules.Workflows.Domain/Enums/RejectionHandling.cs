namespace STRIDE.Modules.Workflows.Domain.Enums;

public enum RejectionHandling
{
    /// <summary>When the approval is rejected the workflow transitions to Halted until manually resolved.</summary>
    HaltWorkflow = 0,

    /// <summary>When rejected the workflow reverts to a specified earlier step so work can be corrected and re-submitted.</summary>
    RevertToStep = 1,
}
