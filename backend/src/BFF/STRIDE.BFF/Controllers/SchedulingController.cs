using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

[ApiController]
[Authorize]
[Route("bff/scheduling/schedules")]
public sealed class SchedulingController : ControllerBase
{
    private readonly SchedulingApiClient           _scheduling;
    private readonly ILogger<SchedulingController> _logger;

    public SchedulingController(SchedulingApiClient scheduling, ILogger<SchedulingController> logger)
    {
        _scheduling = scheduling;
        _logger     = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _scheduling.ListSchedulesAsync(token, cancellationToken), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _scheduling.GetScheduleAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _scheduling.CreateScheduleAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _scheduling.UpdateScheduleAsync(id, body, token, cancellationToken);
        return response.IsSuccessStatusCode ? Ok(await response.Content.ReadAsStringAsync(cancellationToken)) : await ProxyAsync(response, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _scheduling.DeleteScheduleAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _scheduling.ActivateScheduleAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _scheduling.DeactivateScheduleAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetCurrentAccessTokenAsync();

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host scheduling API returned {StatusCode}: {Body}",
                (int)response.StatusCode, json);

            return StatusCode((int)response.StatusCode,
                new ProblemDetails
                {
                    Title  = "Upstream error",
                    Detail = json,
                    Status = (int)response.StatusCode,
                });
        }

        return new ContentResult
        {
            Content     = json,
            ContentType = "application/json",
            StatusCode  = StatusCodes.Status200OK,
        };
    }
}
