using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for the unauthenticated public job endpoints (EP-058 / US-178 &amp; US-179).
///
///   GET  /bff/public/jobs/{token}         — view job status, sign-off state, invoice
///   POST /bff/public/jobs/{token}/signoff  — client sign-off
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("bff/public/jobs")]
public sealed class PublicJobController : ControllerBase
{
    private readonly WorkflowApiClient            _workflows;
    private readonly ILogger<PublicJobController> _logger;

    public PublicJobController(
        WorkflowApiClient workflows,
        ILogger<PublicJobController> logger)
    {
        _workflows = workflows;
        _logger    = logger;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetPublicJobView(string token, CancellationToken cancellationToken)
    {
        var response = await _workflows.GetPublicJobViewAsync(token, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "Public job view: Host returned {StatusCode} for token length {Len}",
                (int)response.StatusCode, token.Length);

            return StatusCode((int)response.StatusCode, new ProblemDetails
            {
                Title  = "Link unavailable",
                Detail = body,
                Status = (int)response.StatusCode,
            });
        }

        return new ContentResult
        {
            Content     = body,
            ContentType = "application/json",
            StatusCode  = StatusCodes.Status200OK,
        };
    }

    [HttpPost("{token}/signoff")]
    public async Task<IActionResult> SignOffPublicJob(string token, CancellationToken cancellationToken)
    {
        using var body = new System.Net.Http.StreamContent(Request.Body);
        body.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = await _workflows.PostPublicSignOffAsync(token, body, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            return StatusCode((int)response.StatusCode, new ProblemDetails
            {
                Title  = "Sign-off failed",
                Detail = detail,
                Status = (int)response.StatusCode,
            });
        }

        return NoContent();
    }
}
