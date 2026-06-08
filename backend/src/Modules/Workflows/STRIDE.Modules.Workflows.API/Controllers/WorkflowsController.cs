using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.API.Dtos;
using STRIDE.Modules.Workflows.Application.Commands.ActivateWorkflow;
using STRIDE.Modules.Workflows.Application.Commands.CancelWorkflow;
using STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;
using STRIDE.Modules.Workflows.Application.Commands.DeleteWorkflow;
using STRIDE.Modules.Workflows.Application.Commands.PauseWorkflow;
using STRIDE.Modules.Workflows.Application.Commands.ResumeWorkflow;
using STRIDE.Modules.Workflows.Application.Commands.StartWorkflow;
using STRIDE.Modules.Workflows.Application.Commands.UpdateWorkflow;
using STRIDE.Modules.Workflows.Application.Queries.ExportStepFieldValuesCsv;
using STRIDE.Modules.Workflows.Application.Queries.GetMyTasks;
using STRIDE.Modules.Workflows.Application.Queries.GetWorkflowDefinition;
using STRIDE.Modules.Workflows.Application.Queries.GetWorkflowInstance;
using STRIDE.Modules.Workflows.Application.Queries.ListWorkflowDefinitions;
using STRIDE.Modules.Workflows.Application.Queries.ListWorkflowInstances;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// Workflow Definitions and Instances.
///
/// Definitions CRUD:
///   GET    /api/workflows               — list all definitions
///   POST   /api/workflows               — create a new Draft definition
///   GET    /api/workflows/{id}          — get a single definition with its steps
///   PUT    /api/workflows/{id}          — update a Draft definition
///   DELETE /api/workflows/{id}          — soft-delete a definition
///   POST   /api/workflows/{id}/activate — transition Draft → Active
///
/// Instance lifecycle:
///   POST   /api/workflows/{id}/start        — start a new instance from an Active definition
///   GET    /api/workflows/{id}/instances    — list instances for a definition
///   GET    /api/workflows/my-tasks           — get all tasks assigned to the current user
///   GET    /api/workflows/instances/{iid}   — get a single instance with its step instances
///   POST   /api/workflows/instances/{iid}/pause   — pause a Running instance
///   POST   /api/workflows/instances/{iid}/resume  — resume a Paused instance
///   POST   /api/workflows/instances/{iid}/cancel  — cancel a Running/Paused instance
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private const string NotFoundFragment = "not found";

    private readonly IMediator     _mediator;
    private readonly ICurrentUser  _currentUser;
    private readonly ITenantContext _tenantContext;

    public WorkflowsController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    // ─── Definitions ──────────────────────────────────────────────────────────

    /// <summary>
    /// List all workflow definitions for the current tenant.
    /// GET /api/workflows
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowDefinitionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDefinitions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListWorkflowDefinitionsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get a single workflow definition by ID, including its step definitions.
    /// GET /api/workflows/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkflowDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDefinition(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkflowDefinitionQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new workflow definition in Draft status.
    /// POST /api/workflows
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWorkflowResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDefinition(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var steps = request.Steps
            .Select(s => new StepRequest(
                s.Name,
                s.Description,
                s.IsRequired,
                s.RequiredRoleId,
                s.FieldDefinitions?
                    .Select(f => new FieldDefinitionRequest(
                        f.Label,
                        f.FieldType,
                        f.IsRequired,
                        f.HelpText,
                        f.DropdownOptions))
                    .ToList()
                    .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        var result = await _mediator.Send(
            new CreateWorkflowCommand(
                TenantId:  _tenantContext.TenantId,
                Name:      request.Name,
                Description: request.Description,
                CreatedBy: _currentUser.UserId,
                Steps:     steps),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Update a Draft workflow definition (name, description).
    /// PUT /api/workflows/{id}
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDefinition(
        Guid id,
        [FromBody] UpdateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateWorkflowCommand(
                WorkflowDefinitionId: id,
                Name:       request.Name,
                Description: request.Description,
                UpdatedBy:  _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Soft-delete a workflow definition.
    /// DELETE /api/workflows/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDefinition(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteWorkflowCommand(
                WorkflowDefinitionId: id,
                DeletedBy: _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Activate a Draft workflow definition (Draft → Active).
    /// POST /api/workflows/{id}/activate
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateDefinition(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ActivateWorkflowCommand(
                WorkflowDefinitionId: id,
                ActivatedBy: _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    // ─── Instances ────────────────────────────────────────────────────────────

    /// <summary>
    /// Start a new workflow instance from an Active definition.
    /// POST /api/workflows/{id}/start
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(StartWorkflowResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartInstance(
        Guid id,
        [FromQuery] Guid? clientId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new StartWorkflowCommand(
                WorkflowDefinitionId: id,
                StartedBy: _currentUser.UserId,
                ClientId:  clientId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// List all workflow instances for the current tenant (across all definitions).
    /// GET /api/workflows/instances
    /// </summary>
    [HttpGet("instances")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowInstanceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAllInstances(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListWorkflowInstancesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Returns all non-terminal step instances assigned to the current user.
    /// GET /api/workflows/my-tasks
    /// </summary>
    [HttpGet("my-tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<MyTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTasks(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMyTasksQuery(
                UserId:   _currentUser.UserId,
                TenantId: _tenantContext.TenantId),
            cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// List all workflow instances for a given definition.
    /// GET /api/workflows/{id}/instances
    /// </summary>
    [HttpGet("{id:guid}/instances")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowInstanceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInstances(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListWorkflowInstancesQuery(id), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get a single workflow instance (with all step instances).
    /// GET /api/workflows/instances/{instanceId}
    /// </summary>
    [HttpGet("instances/{instanceId:guid}")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInstance(Guid instanceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkflowInstanceQuery(instanceId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Pause a Running workflow instance.
    /// POST /api/workflows/instances/{instanceId}/pause
    /// </summary>
    [HttpPost("instances/{instanceId:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseInstance(Guid instanceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new PauseWorkflowCommand(
                WorkflowInstanceId: instanceId,
                PausedBy: _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Resume a Paused workflow instance.
    /// POST /api/workflows/instances/{instanceId}/resume
    /// </summary>
    [HttpPost("instances/{instanceId:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeInstance(Guid instanceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ResumeWorkflowCommand(
                WorkflowInstanceId: instanceId,
                ResumedBy: _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Export all captured step field values for a workflow instance as CSV.
    /// GET /api/workflows/instances/{instanceId}/field-values/csv
    /// </summary>
    [HttpGet("instances/{instanceId:guid}/field-values/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportFieldValuesCsv(Guid instanceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ExportStepFieldValuesCsvQuery(instanceId), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Value!);
        return File(bytes, "text/csv", $"instance-{instanceId:N}-fields.csv");
    }

    /// <summary>
    /// Cancel a Running or Paused workflow instance.
    /// POST /api/workflows/instances/{instanceId}/cancel
    /// </summary>
    [HttpPost("instances/{instanceId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInstance(Guid instanceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelWorkflowCommand(
                WorkflowInstanceId: instanceId,
                CancelledBy: _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
