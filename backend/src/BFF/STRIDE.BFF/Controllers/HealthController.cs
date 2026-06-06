using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for health-check queries.
///
/// Angular calls GET /bff/health — this controller forwards to the Host's
/// /health/ready endpoint and returns the verbose UIResponseWriter JSON.
///
/// No [Authorize] — health status is intentionally public so monitoring
/// dashboards (and the Angular /administration page) can reach it without
/// a session cookie.
/// </summary>
[ApiController]
[Route("bff/health")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthApiClient             _health;
    private readonly ILogger<HealthController>   _logger;

    public HealthController(
        HealthApiClient health,
        ILogger<HealthController> logger)
    {
        _health = health;
        _logger = logger;
    }

    /// <summary>Returns the verbose health JSON from the Host.</summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _health.GetHealthAsync(cancellationToken);
            var json     = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                Content     = json,
                ContentType = "application/json",
                StatusCode  = (int)response.StatusCode,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reach Host health endpoint");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title  = "Health check unavailable",
                    Detail = "Could not reach the STRIDE Host health endpoint.",
                    Status = StatusCodes.Status503ServiceUnavailable,
                });
        }
    }
}
