using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.CompleteStep;

/// <summary>
/// Marks a step instance as Completed. After completion, <c>WorkflowInstance</c>
/// automatically checks if all steps are terminal and completes the workflow if so.
/// </summary>
public sealed record CompleteStepCommand(
    Guid WorkflowInstanceId,
    Guid StepInstanceId,
    Guid CompletedBy) : IRequest<Result>;
