using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.ApproveStep;

/// <summary>
/// Approves the pending approval request on the given step, advancing the workflow.
/// The caller must hold the role specified on the step's definition.
/// </summary>
public sealed record ApproveStepCommand(
    Guid    WorkflowInstanceId,
    Guid    StepInstanceId,
    Guid    ApprovedBy,
    string? Comment = null) : IRequest<Result>;
