using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.ClaimStep;

/// <summary>
/// Self-assignment: a user claims a Pending step for themselves.
/// If the step has a RequiredRoleId the claimant must hold that role.
/// </summary>
public sealed record ClaimStepCommand(
    Guid WorkflowInstanceId,
    Guid StepInstanceId,
    Guid ClaimantId) : IRequest<Result>;
