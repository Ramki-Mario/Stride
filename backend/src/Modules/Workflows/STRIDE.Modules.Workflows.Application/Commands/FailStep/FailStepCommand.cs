using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.FailStep;

/// <summary>
/// Marks a step instance as Failed with a reason. If the step is required,
/// the entire <c>WorkflowInstance</c> is also transitioned to Failed automatically.
/// </summary>
public sealed record FailStepCommand(
    Guid WorkflowInstanceId,
    Guid StepInstanceId,
    string Reason,
    Guid FailedBy) : IRequest<Result>;
