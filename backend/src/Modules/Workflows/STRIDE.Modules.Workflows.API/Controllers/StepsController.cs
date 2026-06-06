using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.API.Dtos;
using STRIDE.Modules.Workflows.Application.Commands.AssignStep;
using STRIDE.Modules.Workflows.Application.Commands.CompleteStep;
using STRIDE.Modules.Workflows.Application.Commands.FailStep;
using STRIDE.Modules.Workflows.Application.Commands.SkipStep;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// Step instance operations scoped to a parent workflow instance.
///
///   POST /api/workflows/instances/{instanceId}/steps/{stepId}/assign   — assign a step
///   POST /api/workflows/instances/{instanceId}/steps/{stepId}/complete  — mark complete
///   POST /api/workflows/instances/{instanceId}/steps/{stepId}/fail      — mark failed (with reason)
///   POST /api/workflows/instances/{instanceId}/steps/{stepId}/skip      — skip a step
///
/// All mutations require an authenticated user; the caller's identity is used as the
/// acting-user argument (assignedBy / completedBy / failedBy / skippedBy).
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflows/instances/{instanceId:guid}/steps")]
public sealed class StepsController : ControllerBase
{
    private readonly IMediator    _mediator;
    private readonly ICurrentUser _currentUser;

    public StepsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator    = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Assign a step to a user.
    /// POST /api/workflows/instances/{instanceId}/steps/{stepId}/assign
    /// </summary>
    [HttpPost("{stepId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid instanceId,
        Guid stepId,
        [FromBody] AssignStepRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignStepCommand(
                WorkflowInstanceId: instanceId,
                StepInstanceId:     stepId,
                AssigneeId:         request.AssigneeId,
                AssignedBy:         _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Mark a step as completed.
    /// POST /api/workflows/instances/{instanceId}/steps/{stepId}/complete
    /// </summary>
    [HttpPost("{stepId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        Guid instanceId,
        Guid stepId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CompleteStepCommand(
                WorkflowInstanceId: instanceId,
                StepInstanceId:     stepId,
                CompletedBy:        _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Mark a step as failed with a reason. If the step is required, the parent
    /// workflow instance is also transitioned to Failed automatically.
    /// POST /api/workflows/instances/{instanceId}/steps/{stepId}/fail
    /// </summary>
    [HttpPost("{stepId:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Fail(
        Guid instanceId,
        Guid stepId,
        [FromBody] FailStepRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new FailStepCommand(
                WorkflowInstanceId: instanceId,
                StepInstanceId:     stepId,
                Reason:             request.Reason,
                FailedBy:           _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Skip a step (only permitted for non-required steps by default).
    /// POST /api/workflows/instances/{instanceId}/steps/{stepId}/skip
    /// </summary>
    [HttpPost("{stepId:guid}/skip")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Skip(
        Guid instanceId,
        Guid stepId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SkipStepCommand(
                WorkflowInstanceId: instanceId,
                StepInstanceId:     stepId,
                SkippedBy:          _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
