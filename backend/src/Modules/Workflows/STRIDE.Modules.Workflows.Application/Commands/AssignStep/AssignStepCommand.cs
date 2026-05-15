using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.AssignStep;

/// <summary>
/// Assigns a step instance to a specific user within a Running workflow.
/// The step must be in Pending status to be assigned.
/// </summary>
public sealed record AssignStepCommand(
    Guid WorkflowInstanceId,
    Guid StepInstanceId,
    Guid AssigneeId,
    Guid AssignedBy) : IRequest<Result>;
