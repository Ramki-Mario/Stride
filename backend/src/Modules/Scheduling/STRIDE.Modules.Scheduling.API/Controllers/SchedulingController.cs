using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Scheduling.API.Models;
using STRIDE.Modules.Scheduling.Application.Commands.ActivateSchedule;
using STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;
using STRIDE.Modules.Scheduling.Application.Commands.DeactivateSchedule;
using STRIDE.Modules.Scheduling.Application.Commands.DeleteSchedule;
using STRIDE.Modules.Scheduling.Application.Commands.UpdateSchedule;
using STRIDE.Modules.Scheduling.Application.Queries.GetScheduleDefinition;
using STRIDE.Modules.Scheduling.Application.Queries.ListScheduleDefinitions;

namespace STRIDE.Modules.Scheduling.API.Controllers;

[ApiController]
[Authorize]
[Route("api/scheduling/schedules")]
public sealed class SchedulingController : ControllerBase
{
    private readonly IMediator   _mediator;
    private readonly ICurrentUser _currentUser;

    public SchedulingController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator    = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListScheduleDefinitionsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetScheduleDefinitionQuery(id), cancellationToken);
        if (result.IsFailure) return NotFound(new { detail = result.Error });
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest req, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateScheduleCommand(req.Name, req.Description, req.WorkflowDefinitionId, req.CronExpression, req.IsActive),
            cancellationToken);

        if (result.IsFailure) return BadRequest(new { detail = result.Error });
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateScheduleRequest req, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateScheduleCommand(id, req.Name, req.Description, req.WorkflowDefinitionId, req.CronExpression),
            cancellationToken);

        if (result.IsFailure) return NotFound(new { detail = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteScheduleCommand(id), cancellationToken);
        if (result.IsFailure) return NotFound(new { detail = result.Error });
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ActivateScheduleCommand(id, _currentUser.UserId), cancellationToken);
        if (result.IsFailure) return BadRequest(new { detail = result.Error });
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivateScheduleCommand(id, _currentUser.UserId), cancellationToken);
        if (result.IsFailure) return BadRequest(new { detail = result.Error });
        return NoContent();
    }
}
