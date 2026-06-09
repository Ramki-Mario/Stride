using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.RejectStep;

/// <summary>
/// Rejects the pending approval request on the given step.
/// Depending on the step's rejection handling, the workflow either halts or reverts to an earlier step.
/// The caller must hold the role specified on the step's definition.
/// </summary>
public sealed record RejectStepCommand(
    Guid    WorkflowInstanceId,
    Guid    StepInstanceId,
    Guid    RejectedBy,
    string? Comment = null) : IRequest<Result>;
