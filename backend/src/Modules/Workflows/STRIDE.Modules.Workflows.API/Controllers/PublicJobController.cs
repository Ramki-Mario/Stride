using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Workflows.Application.Queries.GetPublicJobView;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// Unauthenticated endpoint that resolves a shared-link token and returns a
/// redacted public job status view (EP-058 / US-178).
///
///   GET /api/public/jobs/{token}
///     200 — PublicJobViewDto (link valid; ViewCount incremented)
///     410 — Gone (token not found, expired, or revoked)
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/jobs")]
public sealed class PublicJobController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicJobController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{token}")]
    [ProducesResponseType(typeof(PublicJobViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> GetPublicJobView(string token, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPublicJobViewQuery(token), cancellationToken);

        if (!result.IsSuccess)
            return StatusCode(StatusCodes.Status410Gone, new { detail = result.Error });

        return Ok(result.Value);
    }
}
