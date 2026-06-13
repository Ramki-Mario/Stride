using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.Modules.Workflows.Application.Commands.SignOffPublicJob;
using STRIDE.Modules.Workflows.Application.Queries.GetPublicJobView;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// Unauthenticated endpoints for the client-facing job view (EP-058 / US-178 &amp; US-179).
///
///   GET  /api/public/jobs/{token}        — view job status + sign-off state + invoice
///   POST /api/public/jobs/{token}/signoff — client confirms completion
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

    [HttpPost("{token}/signoff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> SignOffPublicJob(
        string token,
        [FromBody] SignOffPublicJobRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SignOffPublicJobCommand(token, body.ClientName), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == "Sign-off is only available once the job has completed.")
                return BadRequest(new { detail = result.Error });

            return StatusCode(StatusCodes.Status410Gone, new { detail = result.Error });
        }

        return NoContent();
    }
}

public sealed record SignOffPublicJobRequest(string ClientName);
