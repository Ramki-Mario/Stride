namespace STRIDE.Modules.Workflows.Application.Commands.StartWorkflow;

/// <summary>Returned after a workflow instance is successfully started.</summary>
public sealed record StartWorkflowResult(
    Guid WorkflowInstanceId,
    Guid WorkflowDefinitionId,
    string WorkflowName,
    int StepCount);
