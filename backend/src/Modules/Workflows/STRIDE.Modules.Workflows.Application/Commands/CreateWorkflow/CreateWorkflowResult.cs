namespace STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;

/// <summary>Returned after a workflow definition is successfully created.</summary>
public sealed record CreateWorkflowResult(
    Guid WorkflowDefinitionId,
    string Name,
    int StepCount);
