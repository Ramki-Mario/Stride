using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Workflows.Application.Queries.GetActivityTimeline;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// Activity timeline scoped to a workflow instance.
///
///   GET /api/workflows/instances/{instanceId}/activity?page=1&amp;pageSize=20&amp;order=asc
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflows/instances/{instanceId:guid}/activity")]
public sealed class ActivityController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActivityController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>
    /// Returns a page of activity events for the given workflow instance.
    /// GET /api/workflows/instances/{instanceId}/activity?page=1&amp;pageSize=20&amp;order=asc
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivityTimeline(
        Guid instanceId,
        [FromQuery] int    page      = 1,
        [FromQuery] int    pageSize  = 20,
        [FromQuery] string order     = "asc",
        CancellationToken cancellationToken = default)
    {
        var ascending = !order.Equals("desc", StringComparison.OrdinalIgnoreCase);

        var result = await _mediator.Send(
            new GetActivityTimelineQuery(instanceId, page, pageSize, ascending),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
